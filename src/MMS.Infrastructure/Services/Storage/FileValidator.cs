using MMS.Application.Common;
using MMS.Application.DTOs.ImageFile;
using MMS.Application.Interfaces.Services;

namespace MMS.Infrastructure.Services.Storage
{
    public class FileValidator : IFileValidator
    {
        public Task<Result<ImageFileInfo>> ValidateImageAsync(Stream stream, string fileName, int maxSizeMb)
        {
            if (stream == null || stream.Length == 0)
            {
                return Task.FromResult(Result<ImageFileInfo>.Failure("INVALID_FILE", "File is empty or null"));
            }

            long maxSizeBytes = maxSizeMb * 1024 * 1024;
            if (stream.Length > maxSizeBytes)
            {
                return Task.FromResult(Result<ImageFileInfo>.Failure("FILE_TOO_LARGE", $"File size exceeds the maximum limit of {maxSizeMb} MB"));
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".webp")
            {
                return Task.FromResult(Result<ImageFileInfo>.Failure("INVALID_EXTENSION", "Only JPG, PNG and WebP files are allowed"));
            }

            // Check magic bytes
            byte[] header = new byte[12];
            stream.Position = 0;
            var bytesRead = stream.Read(header, 0, 12);
            stream.Position = 0; // Reset stream position!

            if (bytesRead < 4)
            {
                return Task.FromResult(Result<ImageFileInfo>.Failure("INVALID_FILE", "File is too short to be a valid image"));
            }

            string detectedFormat = string.Empty;

            // JPEG: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                detectedFormat = "jpg";
            }
            // PNG: 89 50 4E 47 0D 0A 1A 0A
            else if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            {
                detectedFormat = "png";
            }
            // WebP: RIFF ... WEBP
            else if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                     header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
            {
                detectedFormat = "webp";
            }
            else
            {
                return Task.FromResult(Result<ImageFileInfo>.Failure("INVALID_FORMAT", "File content does not match its extension or is not a supported image format"));
            }

            return Task.FromResult(Result<ImageFileInfo>.Success(new ImageFileInfo(detectedFormat, stream.Length)));
        }
    }
}
