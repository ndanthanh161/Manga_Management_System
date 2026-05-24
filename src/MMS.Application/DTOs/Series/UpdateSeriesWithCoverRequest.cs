using Microsoft.AspNetCore.Http;
using System;

namespace MMS.Application.DTOs.Series;

public class UpdateSeriesWithCoverRequest
{
    public IFormFile File { get; set; } = null!;
    public string? Title { get; set; }
    public string? AlternativeTitle { get; set; }
    public string? Description { get; set; }
}
