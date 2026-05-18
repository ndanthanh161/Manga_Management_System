namespace MMS.Application.DTOs.Page;

public record CreatePageRequest(
    int PageNumber,
    string ImageUrl,
    Guid ChapterId
);
