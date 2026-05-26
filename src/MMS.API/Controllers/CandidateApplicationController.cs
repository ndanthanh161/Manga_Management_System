using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMS.Application.DTOs.CandidateApplication;
using MMS.Application.Interfaces.Services;
using MMS.Application.Common;
using System.Threading.Tasks;
using System.Threading;

namespace MMS.API.Controllers;

[ApiController]
[Route("api/candidate-application")]
public class CandidateApplicationController : ControllerBase
{
    private readonly ICandidateApplicationService _service;

    public CandidateApplicationController(ICandidateApplicationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Tác giả vô danh gửi đơn ứng tuyển kèm bản thảo qua Multipart Form Data
    /// </summary>
    [HttpPost("submit")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MMS.Application.Common.Result<CandidateApplicationDto>), 200)]
    [ProducesResponseType(typeof(MMS.Application.Common.Result<CandidateApplicationDto>), 400)]
    public async Task<IActionResult> SubmitApplication(
        [FromForm] SubmitApplicationFormRequest form,
        CancellationToken ct)
    {
        if (form.Files == null || form.Files.Count == 0)
        {
            return BadRequest(MMS.Application.Common.Result<CandidateApplicationDto>.Failure(
                "FILE_REQUIRED",
                "Ít nhất một file bản thảo là bắt buộc."));
        }

        var request = new SubmitApplicationRequest(
            form.Email,
            form.Password,
            form.FullName,
            form.SeriesTitle,
            form.Files
        );

        var result = await _service.SubmitApplicationAsync(request, ct);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }
}
