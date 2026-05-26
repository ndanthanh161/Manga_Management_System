using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace MMS.Application.DTOs.CandidateApplication;

public class SubmitApplicationFormRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string SeriesTitle { get; set; } = null!;
    public List<IFormFile> Files { get; set; } = null!;
}
