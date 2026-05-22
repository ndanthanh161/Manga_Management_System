using Mapster;
using MMS.Application.Common;
using MMS.Application.DTOs.Page;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;

namespace MMS.Application.Services;

public class PageService : IPageService
{
    private readonly IUnitOfWork _uow;
    private readonly IImageStorageService _imageStorageService;
    private readonly IFileValidator _fileValidator;

    public PageService(
        IUnitOfWork uow,
        IImageStorageService imageStorageService,
        IFileValidator fileValidator)
    {
        _uow = uow;
        _imageStorageService = imageStorageService;
        _fileValidator = fileValidator;
    }

    public async Task<PageResponse> CreateAsync(CreatePageRequest request)
    {
        var chapter = await _uow.Chapters.GetByIdAsync(request.ChapterId)
            ?? throw new KeyNotFoundException($"Chapter {request.ChapterId} not found.");

        var page = request.Adapt<Page>();

        await _uow.Pages.AddAsync(page);
        await _uow.SaveChangesAsync();

        return page.Adapt<PageResponse>();
    }

    public async Task<IEnumerable<PageResponse>> GetByChapterAsync(Guid chapterId)
    {
        var list = await _uow.Pages.GetCurrentPagesByChapterAsync(chapterId);
        return list.Adapt<IEnumerable<PageResponse>>();
    }

    public async Task<Result<PageResponse>> UploadPageVersionAsync(Guid chapterId, int pageNumber, 
        Stream fileStream, string fileName, Guid userId, CancellationToken ct = default)
    {
        var chapter = await _uow.Chapters.GetByIdAsync(chapterId);
        if (chapter == null)
            return Result<PageResponse>.NotFound("CHAPTER_NOT_FOUND", "Chapter not found.");

        var series = await _uow.Series.GetByIdAsync(chapter.SeriesId);
        if (series == null)
            return Result<PageResponse>.NotFound("SERIES_NOT_FOUND", "Series not found.");

        if (series.AuthorId != userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null || user.MangakaId != series.AuthorId)
            {
                return Result<PageResponse>.Forbidden("UNAUTHORIZED_UPLOAD", "You do not have permission to upload pages for this series.");
            }
        }

        var validationResult = await _fileValidator.ValidateImageAsync(fileStream, fileName, 20);
        if (!validationResult.Succeeded)
            return Result<PageResponse>.Failure("INVALID_FILE", validationResult.Message ?? "Invalid image file.");

        var oldPage = await _uow.Pages.GetCurrentByChapterAndNumberAsync(chapterId, pageNumber);
        int nextVersion = 1;
        if (oldPage != null)
        {
            nextVersion = oldPage.Version + 1;
            oldPage.IsCurrentVersion = false;
            _uow.Pages.Update(oldPage);
        }

        var folder = $"mms/chapter_{chapterId}";
        var hint = $"page_{pageNumber}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        
        // Seek stream back to beginning after validation
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        var uploadResult = await _imageStorageService.UploadImageAsync(fileStream, folder, hint, ct);
        if (!uploadResult.Succeeded || uploadResult.Data == null)
            return Result<PageResponse>.Failure("UPLOAD_FAILED", uploadResult.Message ?? "Upload to Cloudinary failed.");

        var newPage = new Page
        {
            Id = Guid.NewGuid(),
            ChapterId = chapterId,
            PageNumber = pageNumber,
            ImageUrl = uploadResult.Data.SecureUrl,
            CloudinaryPublicId = uploadResult.Data.PublicId,
            Width = uploadResult.Data.Width,
            Height = uploadResult.Data.Height,
            FileSizeBytes = uploadResult.Data.FileSizeBytes,
            Version = nextVersion,
            IsCurrentVersion = true,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Pages.AddAsync(newPage);
        await _uow.SaveChangesAsync();

        return Result<PageResponse>.Success(newPage.Adapt<PageResponse>());
    }

    public async Task<Result<List<PageResponse>>> GetPageHistoryAsync(Guid chapterId, int pageNumber)
    {
        var list = await _uow.Pages.GetPageHistoryAsync(chapterId, pageNumber);
        return Result<List<PageResponse>>.Success(list.Adapt<List<PageResponse>>());
    }
}
