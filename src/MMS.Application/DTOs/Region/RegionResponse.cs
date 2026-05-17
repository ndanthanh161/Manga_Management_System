namespace MMS.Application.DTOs.Region;

public record RegionResponse(
    Guid Id,
    string Name,
    string Description,
    Guid? PageId,
    double X,
    double Y,
    double Width,
    double Height
);
