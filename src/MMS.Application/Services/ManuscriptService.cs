using Mapster;
using MMS.Application.Common;
using MMS.Application.DTOs.Manuscript;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Application.Services;

public class ManuscriptService : IManuscriptService
{
    private readonly IUnitOfWork _uow;

    public ManuscriptService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<ManuscriptResponse>> SnapshotChapterAsync(Guid chapterId, string name, Guid userId)
    {
        var chapter = await _uow.Chapters.GetByIdAsync(chapterId);
        if (chapter == null)
            return Result<ManuscriptResponse>.NotFound("CHAPTER_NOT_FOUND", "Chapter not found.");

        var series = await _uow.Series.GetByIdAsync(chapter.SeriesId);
        if (series == null)
            return Result<ManuscriptResponse>.NotFound("SERIES_NOT_FOUND", "Series not found.");

        // Check ownership (Mangaka or their Assistant)
        if (series.AuthorId != userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null || user.MangakaId != series.AuthorId)
            {
                return Result<ManuscriptResponse>.Forbidden("UNAUTHORIZED_ACTION", "You do not have permission to snapshot this chapter.");
            }
        }

        var currentPages = await _uow.Pages.GetCurrentPagesByChapterAsync(chapterId);
        if (currentPages == null || !currentPages.Any())
        {
            return Result<ManuscriptResponse>.Failure("NO_PAGES", "Cannot snapshot a chapter with no pages.");
        }

        var existingManuscript = await _uow.Manuscripts.GetCurrentByChapterAsync(chapterId);
        int nextVersion = 1;
        if (existingManuscript != null)
        {
            nextVersion = existingManuscript.Version + 1;
            existingManuscript.IsCurrentVersion = false;
            _uow.Manuscripts.Update(existingManuscript);
        }

        var newManuscript = new Manuscript
        {
            Id = Guid.NewGuid(),
            Name = name,
            Status = ManuscriptStatus.Draft,
            Version = nextVersion,
            IsCurrentVersion = true,
            ParentManuscriptId = existingManuscript?.Id,
            ChapterId = chapterId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Manuscripts.AddAsync(newManuscript);

        foreach (var page in currentPages)
        {
            var pageRef = new ManuscriptPageRef
            {
                Id = Guid.NewGuid(),
                ManuscriptId = newManuscript.Id,
                PageId = page.Id,
                PageVersion = page.Version
            };
            await _uow.ManuscriptPageRefs.AddAsync(pageRef);
        }

        await _uow.SaveChangesAsync();

        return Result<ManuscriptResponse>.Success(newManuscript.Adapt<ManuscriptResponse>());
    }

    public async Task<Result<ManuscriptResponse>> CreateNewVersionAsync(Guid currentManuscriptId, Guid userId)
    {
        var currentManuscript = await _uow.Manuscripts.GetByIdAsync(currentManuscriptId);
        if (currentManuscript == null)
            return Result<ManuscriptResponse>.NotFound("MANUSCRIPT_NOT_FOUND", "Manuscript not found.");

        var chapter = await _uow.Chapters.GetByIdAsync(currentManuscript.ChapterId);
        if (chapter == null)
            return Result<ManuscriptResponse>.NotFound("CHAPTER_NOT_FOUND", "Chapter not found.");

        var series = await _uow.Series.GetByIdAsync(chapter.SeriesId);
        if (series == null)
            return Result<ManuscriptResponse>.NotFound("SERIES_NOT_FOUND", "Series not found.");

        // Check ownership
        if (series.AuthorId != userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null || user.MangakaId != series.AuthorId)
            {
                return Result<ManuscriptResponse>.Forbidden("UNAUTHORIZED_ACTION", "You do not have permission to create a new version of this manuscript.");
            }
        }

        if (!currentManuscript.IsCurrentVersion)
        {
            return Result<ManuscriptResponse>.Failure("NOT_CURRENT_VERSION", "Can only version from the current latest manuscript version.");
        }

        currentManuscript.IsCurrentVersion = false;
        _uow.Manuscripts.Update(currentManuscript);

        var newManuscript = new Manuscript
        {
            Id = Guid.NewGuid(),
            Name = currentManuscript.Name,
            Status = ManuscriptStatus.Draft,
            Version = currentManuscript.Version + 1,
            IsCurrentVersion = true,
            ParentManuscriptId = currentManuscript.Id,
            ChapterId = currentManuscript.ChapterId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Manuscripts.AddAsync(newManuscript);

        var oldRefs = await _uow.ManuscriptPageRefs.GetByManuscriptAsync(currentManuscriptId);
        foreach (var oldRef in oldRefs)
        {
            var newRef = new ManuscriptPageRef
            {
                Id = Guid.NewGuid(),
                ManuscriptId = newManuscript.Id,
                PageId = oldRef.PageId,
                PageVersion = oldRef.PageVersion
            };
            await _uow.ManuscriptPageRefs.AddAsync(newRef);
        }

        await _uow.SaveChangesAsync();

        return Result<ManuscriptResponse>.Success(newManuscript.Adapt<ManuscriptResponse>());
    }

    public async Task<Result<List<ManuscriptResponse>>> GetVersionHistoryAsync(Guid manuscriptId)
    {
        var list = await _uow.Manuscripts.GetVersionHistoryAsync(manuscriptId);
        if (!list.Any())
            return Result<List<ManuscriptResponse>>.NotFound("MANUSCRIPT_NOT_FOUND", "Manuscript history not found.");

        return Result<List<ManuscriptResponse>>.Success(list.Adapt<List<ManuscriptResponse>>());
    }

    public async Task<Result<ManuscriptResponse>> RevertToVersionAsync(Guid versionId, Guid userId)
    {
        var target = await _uow.Manuscripts.GetByIdAsync(versionId);
        if (target == null)
            return Result<ManuscriptResponse>.NotFound("MANUSCRIPT_NOT_FOUND", "Target manuscript not found.");

        var chapter = await _uow.Chapters.GetByIdAsync(target.ChapterId);
        if (chapter == null)
            return Result<ManuscriptResponse>.NotFound("CHAPTER_NOT_FOUND", "Chapter not found.");

        var series = await _uow.Series.GetByIdAsync(chapter.SeriesId);
        if (series == null)
            return Result<ManuscriptResponse>.NotFound("SERIES_NOT_FOUND", "Series not found.");

        // Check ownership
        if (series.AuthorId != userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null || user.MangakaId != series.AuthorId)
            {
                return Result<ManuscriptResponse>.Forbidden("UNAUTHORIZED_ACTION", "You do not have permission to revert this manuscript.");
            }
        }

        if (target.IsCurrentVersion)
        {
            return Result<ManuscriptResponse>.Success(target.Adapt<ManuscriptResponse>());
        }

        var allVersions = await _uow.Manuscripts.GetVersionHistoryAsync(versionId);
        foreach (var version in allVersions)
        {
            if (version.Id == target.Id)
            {
                version.IsCurrentVersion = true;
            }
            else if (version.IsCurrentVersion)
            {
                version.IsCurrentVersion = false;
            }
            _uow.Manuscripts.Update(version);
        }

        await _uow.SaveChangesAsync();

        return Result<ManuscriptResponse>.Success(target.Adapt<ManuscriptResponse>());
    }

    public async Task<Result<ManuscriptResponse>> GetByIdAsync(Guid id)
    {
        var manuscript = await _uow.Manuscripts.GetByIdAsync(id);
        if (manuscript == null)
            return Result<ManuscriptResponse>.NotFound("MANUSCRIPT_NOT_FOUND", "Manuscript not found.");

        return Result<ManuscriptResponse>.Success(manuscript.Adapt<ManuscriptResponse>());
    }
}
