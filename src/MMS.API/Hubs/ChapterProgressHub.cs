namespace MMS.API.Hubs;

public class ChapterProgressHub : BaseAuthHub
{
    public async Task JoinChapter(Guid chapterId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"progress:chapter:{chapterId}");
    }

    public async Task LeaveChapter(Guid chapterId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"progress:chapter:{chapterId}");
    }

    // Server → Client events:
    // "pageStatusChanged"       : { pageId, pageNumber, status }
    // "chapterProgressUpdated"  : { chapterId, totalPages, completedPages, percent }
}
