using Microsoft.AspNetCore.SignalR;
using MMS.Application.Interfaces;

namespace MMS.API.Hubs;

public class AnnotationHub : BaseAuthHub
{
    private readonly IUnitOfWork _uow;

    public AnnotationHub(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task JoinChapter(Guid chapterId)
    {
        // Validate: chapter tồn tại
        var chapter = await _uow.Chapters.GetByIdAsync(chapterId);
        if (chapter == null)
            throw new HubException("Chapter not found");

        // Validate: user có quyền xem chapter này
        // (là author của series, hoặc TantouEditorId của series)
        var series = await _uow.Series.GetByIdAsync(chapter.SeriesId);
        bool hasAccess = series != null &&
            (series.AuthorId == CurrentUserId ||
             series.TantouEditorId == CurrentUserId);

        if (!hasAccess)
            throw new HubException("Access denied to this chapter");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"chapter:{chapterId}");
    }

    public async Task LeaveChapter(Guid chapterId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chapter:{chapterId}");
    }

    // Server → Client events (broadcast từ EventHandlers):
    // "annotationAdded"        : { annotationId, pageId, category, x, y, width, height, createdBy }
    // "annotationStatusChanged": { annotationId, oldStatus, newStatus, actorId }
}
