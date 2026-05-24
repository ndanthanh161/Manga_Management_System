using Microsoft.AspNetCore.Http;

namespace MMS.Application.DTOs.Series;

public class CreateSeriesWithCoverRequest
{
    public IFormFile File { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? AlternativeTitle { get; set; }
    public string? AuthorEmail { get; set; }
    public string? AuthorName { get; set; }
}
