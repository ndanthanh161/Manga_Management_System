using Mapster;
using MMS.Application.Common;
using MMS.Application.DTOs.Page;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MMS.Application.Services;

public class PageService : IPageService
{
    private readonly IUnitOfWork _uow;
    private readonly IImageStorageService _imageStorage;
    private readonly IFileValidator _fileValidator;

    public PageService(IUnitOfWork uow, IImageStorageService imageStorage, IFileValidator fileValidator)
    {
        _uow = uow;
        _imageStorage = imageStorage;
        _fileValidator = fileValidator;
    }

    public async Task<PageResponse> CreateWithImageAsync(
        Stream imageStream, Guid chapterId, int pageNumber, CancellationToken ct = default)
    {
        // 1. Validate chapter
        var chapter = await _uow.Chapters.GetByIdAsync(chapterId)
            ?? throw new KeyNotFoundException($"Chapter {chapterId} not found.");

        // 2. Upload lên Cloudinary
        var folder = $"mms/chapter_{chapterId}";
        var hint = $"page_{pageNumber}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        var uploadResult = await _imageStorage.UploadImageAsync(imageStream, folder, hint, ct);

        if (!uploadResult.Succeeded)
            throw new InvalidOperationException($"Upload failed: {uploadResult.Message}");

        // 3. Tạo record Page bằng Mapster (Map sạch sẽ từ file mapping)
        try
        {
            var page = uploadResult.Data!.Adapt<Page>();
            page.ChapterId = chapterId;
            page.PageNumber = pageNumber;

            await _uow.Pages.AddAsync(page);
            await _uow.SaveChangesAsync();

            return page.Adapt<PageResponse>();
        }
        catch
        {
            await _imageStorage.DeleteImageAsync(uploadResult.Data!.PublicId, ct);
            throw;
        }
    }

    public async Task<IEnumerable<PageResponse>> GetByChapterAsync(Guid chapterId)
    {
        var list = await _uow.Pages.FindAsync(p => p.ChapterId == chapterId);
        return list.OrderBy(p => p.PageNumber).Adapt<IEnumerable<PageResponse>>();
    }

    public async Task<Result<PageResponse>> UploadPageAsync(UploadPageRequest request, CancellationToken ct = default)
    {
        if (request.File == null || request.File.Length == 0)
            return Result<PageResponse>.Failure("FILE_EMPTY", "File is empty.");

        var validationResult = await _fileValidator.ValidateImageAsync(request.File.OpenReadStream(), request.File.FileName, 20);
        if (!validationResult.Succeeded)
            return Result<PageResponse>.Failure(validationResult.ErrorCode ?? "VALIDATION_FAILED", validationResult.Message ?? "File validation failed.");

        using var stream = request.File.OpenReadStream();
        try
        {
            var page = await CreateWithImageAsync(stream, request.ChapterId, request.PageNumber, ct);
            return Result<PageResponse>.Success(page);
        }
        catch (KeyNotFoundException ex)
        {
            return Result<PageResponse>.NotFound("CHAPTER_NOT_FOUND", ex.Message);
        }
        catch (Exception ex)
        {
            return Result<PageResponse>.Failure("UPLOAD_ERROR", ex.Message);
        }
    }
}
