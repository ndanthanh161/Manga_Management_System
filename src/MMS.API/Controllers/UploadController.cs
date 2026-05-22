using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMS.API.Authorization;
using MMS.Application.DTOs.ImageFile;
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
    public async Task<IActionResult> UploadPage(
        [FromForm] IFormFile file,
        [FromQuery] Guid chapterId,
        [FromQuery] int pageNumber,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var userId = Guid.Parse(userIdStr);

        using var stream = file.OpenReadStream();
        var result = await _pageService.UploadPageVersionAsync(chapterId, pageNumber, stream, file.FileName, userId, ct);

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
    public async Task<IActionResult> UploadCover(
        [FromForm] IFormFile file,
        [FromQuery] Guid seriesId,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        var userId = Guid.Parse(userIdStr);

        // Check if user is Editorial Board or has rights to manage the series
        bool isAuthorized = User.IsInRole("Editorial Board") || await _seriesService.CanUserManageSeriesAsync(seriesId, userId);
        if (!isAuthorized) return Forbid();

        var validationResult = await _fileValidator.ValidateImageAsync(file.OpenReadStream(), file.FileName, 10);
        if (!validationResult.Succeeded) return BadRequest(validationResult.Message);

        var folder = $"mms/series_{seriesId}";
        var hint = $"cover_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

        using var stream = file.OpenReadStream();
        var uploadResult = await _imageStorageService.UploadImageAsync(stream, folder, hint, ct);

        if (!uploadResult.Succeeded) return StatusCode(500, uploadResult.Message);

        var urls = _imageStorageService.BuildAllUrls(uploadResult.Data!.PublicId);
        return Ok(new { upload = uploadResult.Data, urls = urls });
    }

    [HttpPost("avatar")]
    [Authorize]
    public async Task<IActionResult> UploadAvatar(
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest("File is empty.");

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

        var validationResult = await _fileValidator.ValidateImageAsync(file.OpenReadStream(), file.FileName, 5);
        if (!validationResult.Succeeded) return BadRequest(validationResult.Message);

        var folder = "mms/avatars";
        var hint = $"{userIdStr}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

        using var stream = file.OpenReadStream();
        var uploadResult = await _imageStorageService.UploadImageAsync(stream, folder, hint, ct);

        if (!uploadResult.Succeeded) return StatusCode(500, uploadResult.Message);

        var urls = _imageStorageService.BuildAllUrls(uploadResult.Data!.PublicId);
        return Ok(new { upload = uploadResult.Data, urls = urls });
    }
}
