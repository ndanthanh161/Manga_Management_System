using Microsoft.AspNetCore.Http;

namespace MMS.Application.DTOs.CandidateApplication;

public record SubmitApplicationRequest(
    string Email,
    string Password,
    string FullName,
    string SeriesTitle,
    List<IFormFile> Files
);