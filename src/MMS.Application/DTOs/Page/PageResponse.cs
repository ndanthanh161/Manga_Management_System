namespace MMS.Application.DTOs.Page;

public record PageResponse(
    Guid Id,
    int PageNumber,
    string ImageUrl,
    Guid ChapterId
);
