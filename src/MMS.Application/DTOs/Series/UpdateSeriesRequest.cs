using Microsoft.AspNetCore.Http;
using MMS.Domain.Enums;

namespace MMS.Application.DTOs.Series;

public record UpdateSeriesRequest(
    string? Title = null,
    string? AlternativeTitle = null,
    string? Description = null,
    string? CoverImageUrl = null,
    IFormFile? File = null,
    SeriesStatus? Status = null
);
