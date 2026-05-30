using Microsoft.AspNetCore.Http;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Application.DTOs.Series;

public record CreateSeriesRequest(
    string Title = "",
    string Description = "",
    string? AlternativeTitle = null,
    string? CoverImageUrl = null,
    IFormFile? File = null,
    string? AuthorEmail = null,
    string? AuthorName = null,
    SeriesStatus? Status = null
);
