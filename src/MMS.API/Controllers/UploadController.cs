using Microsoft.AspNetCore.Mvc;
using MMS.Application.DTOs.Page;
using MMS.Application.DTOs.Series;
using MMS.Application.DTOs.User;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;

namespace MMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IPageService _pageService;
    private readonly ISeriesService _seriesService;
    private readonly IUserService _userService;

    public UploadController(
        IPageService pageService,
        ISeriesService seriesService,
        IUserService userService)
    {
        _pageService = pageService;
        _seriesService = seriesService;
        _userService = userService;
    }

    [HttpPost("page")]
    public async Task<IActionResult> UploadPage(
        [FromForm] UploadPageRequest request,
        CancellationToken ct)
    {
        var result = await _pageService.UploadPageAsync(request, ct);
        return HandleResult(result);
    }

    [HttpPost("cover")]
    public async Task<IActionResult> CreateSeriesWithCover(
        [FromForm] CreateSeriesWithCoverRequest request,
        CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? mangakaId = string.IsNullOrEmpty(userIdStr) ? null : Guid.Parse(userIdStr);

        var result = await _seriesService.CreateSeriesWithCoverAsync(mangakaId, request, ct);
        return HandleResult(result);
    }

    [HttpPut("cover")]
    public async Task<IActionResult> UpdateSeriesWithCover(
        [FromQuery] Guid seriesId,
        [FromForm] UpdateSeriesWithCoverRequest request,
        CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var userId = Guid.Parse(userIdStr);

        var result = await _seriesService.UpdateSeriesWithCoverAsync(seriesId, userId, request, ct);
        return HandleResult(result);
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(
        [FromForm] UploadAvatarRequest request,
        CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var userId = Guid.Parse(userIdStr);

        var result = await _userService.UploadAvatarAsync(userId, request, ct);
        return HandleResult(result);
    }

    [NonAction]
    protected IActionResult HandleResult<T>(MMS.Application.Common.Result<T> result)
    {
        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new { errorCode = result.ErrorCode, message = result.Message, errors = result.Errors });
        }
        return Ok(result.Data);
    }
}
