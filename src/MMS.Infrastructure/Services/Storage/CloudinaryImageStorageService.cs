using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MMS.Application.Common;
using MMS.Application.DTOs.ImageFile;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Enums;

namespace MMS.Infrastructure.Services.Storage;

public class CloudinaryImageStorageService : IImageStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _options;
    private readonly ILogger<CloudinaryImageStorageService> _logger;

    public CloudinaryImageStorageService(
        Cloudinary cloudinary,
        IOptions<CloudinaryOptions> options,
        ILogger<CloudinaryImageStorageService> logger)
    {
        _cloudinary = cloudinary;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<UploadedImageDto>> UploadImageAsync(Stream stream, string folder, string publicIdHint, CancellationToken ct = default)
    {
        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(publicIdHint, stream),
                Folder = folder,
                PublicId = publicIdHint,
                Overwrite = false,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams, ct);

            if (result.Error != null)
            {
                _logger.LogError("Cloudinary upload failed: {Error}", result.Error.Message);
                return Result<UploadedImageDto>.Failure("UPLOAD_FAILED", result.Error.Message);
            }

            var dto = new UploadedImageDto(
                result.PublicId,
                result.SecureUrl.ToString(),
                result.Format,
                result.Width,
                result.Height,
                result.Bytes
            );

            return Result<UploadedImageDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Cloudinary upload");
            return Result<UploadedImageDto>.Failure("UPLOAD_EXCEPTION", "An error occurred during upload");
        }
    }

    public async Task<Result<bool>> DeleteImageAsync(string publicId, CancellationToken ct = default)
    {
        try
        {
            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            if (result.Result == "ok")
            {
                return Result<bool>.Success(true);
            }
            
            _logger.LogWarning("Cloudinary delete failed for {PublicId}: {Result}", publicId, result.Result);
            return Result<bool>.Failure("DELETE_FAILED", $"Failed to delete image: {result.Result}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Cloudinary delete for {PublicId}", publicId);
            return Result<bool>.Failure("DELETE_EXCEPTION", "An error occurred during deletion");
        }
    }

    public string GetTransformedUrl(string publicId, ImageVariant variant)
    {
        return CloudinaryUrlBuilder.Build(_options.CloudName, publicId, variant);
    }

    public ImageUrlsDto BuildAllUrls(string publicId)
    {
        return new ImageUrlsDto(
            publicId,
            GetTransformedUrl(publicId, ImageVariant.Original),
            GetTransformedUrl(publicId, ImageVariant.Large1024),
            GetTransformedUrl(publicId, ImageVariant.Medium512),
            GetTransformedUrl(publicId, ImageVariant.Thumbnail256)
        );
    }
}
