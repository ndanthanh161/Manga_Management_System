namespace MMS.Domain.Enums;

public enum AppTaskStatus
{
    Todo,
    InProgress,
    Review,        // Assistant nộp xong, chờ Mangaka xem xét
    NeedsRevision, // Mangaka yêu cầu sửa lại (có Annotation đi kèm)
    Done,
    Canceled
}
