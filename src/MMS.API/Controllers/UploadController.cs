using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMS.API.Authorization;
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
    private readonly IImageStorageService _imageStorageService;
    private readonly IFileValidator _fileValidator;
    private readonly ISeriesService _seriesService;
    private readonly IPageService _pageService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        IImageStorageService imageStorageService,
        IFileValidator fileValidator,
        ISeriesService seriesService,
        IPageService pageService,
        ILogger<UploadController> logger)
    {
        _imageStorageService = imageStorageService;
        _fileValidator = fileValidator;
        _seriesService = seriesService;
        _pageService = pageService;
        _logger = logger;
    }

    [HttpPost("page")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPage(
        [FromForm] UploadPageRequest request,
        CancellationToken ct)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var userId = Guid.Parse(userIdStr);

        using var stream = request.File.OpenReadStream();
        var result = await _pageService.UploadPageVersionAsync(
            request.ChapterId,
            request.PageNumber,
            stream,
            request.File.FileName,
            userId,
            ct);

        if (!result.Succeeded)
        {
            if (result.StatusCode == 404) return NotFound(result.Message);
            if (result.StatusCode == 403) return Forbid();
            return BadRequest(result.Message);
        }

        var pageResponse = result.Data!;
        var urls = _imageStorageService.BuildAllUrls(pageResponse.CloudinaryPublicId ?? "");

        return Ok(new
        {
            page = pageResponse,
            urls = urls
        });
    }

    [HttpPost("cover")]
    [Authorize(Policy = PolicyNames.CanManageSeries)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCover(
        [FromForm] UploadCoverRequest request,
        CancellationToken ct)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var userId = Guid.Parse(userIdStr);

        // Check if user is Editorial Board or has rights to manage the series
        bool isAuthorized = User.IsInRole("Editorial Board") || await _seriesService.CanUserManageSeriesAsync(request.SeriesId, userId);
        if (!isAuthorized) return Forbid();

        var validationResult = await _fileValidator.ValidateImageAsync(request.File.OpenReadStream(), request.File.FileName, 10);
        if (!validationResult.Succeeded) return BadRequest(validationResult.Message);

        var folder = $"mms/series_{request.SeriesId}";
        var hint = $"cover_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

        using var stream = request.File.OpenReadStream();
        var uploadResult = await _imageStorageService.UploadImageAsync(stream, folder, hint, ct);

        if (!uploadResult.Succeeded) return StatusCode(500, uploadResult.Message);

        var urls = _imageStorageService.BuildAllUrls(uploadResult.Data!.PublicId);
        return Ok(new { upload = uploadResult.Data, urls = urls });
    }

    [HttpPost("series")]
    [Authorize(Roles = "Mangaka,Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateSeries(
        [FromForm] CreateSeriesRequest request,
        CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var userId = Guid.Parse(userIdStr);

        var result = await _seriesService.CreateAsync(userId, request, ct);

        if (!result.Succeeded)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpPost("avatar")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar(
        [FromForm] UploadAvatarRequest request,
        CancellationToken ct)
    {
        if (request.File == null || request.File.Length == 0) return BadRequest("File is empty.");

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

        var validationResult = await _fileValidator.ValidateImageAsync(request.File.OpenReadStream(), request.File.FileName, 5);
        if (!validationResult.Succeeded) return BadRequest(validationResult.Message);

        var folder = "mms/avatars";
        var hint = $"{userIdStr}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

        using var stream = request.File.OpenReadStream();
        var uploadResult = await _imageStorageService.UploadImageAsync(stream, folder, hint, ct);

        if (!uploadResult.Succeeded) return StatusCode(500, uploadResult.Message);

        var urls = _imageStorageService.BuildAllUrls(uploadResult.Data!.PublicId);
        return Ok(new { upload = uploadResult.Data, urls = urls });
    }
}
