using Microsoft.AspNetCore.Http;

namespace MMS.Application.DTOs.Series;

public record CreateSeriesRequest(
    string Title,
    string Description,
    string? AlternativeTitle = null,
    string? CoverImageUrl = null,
    IFormFile? File = null,
    string? AuthorEmail = null,
    string? AuthorName = null
);
