using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Application.DTOs.ImageFile
{
    public record UploadedImageDto(
    string PublicId,       // Source of truth — lưu DB
    string SecureUrl,      // URL original (convenience)
    string Format,         // "jpg" | "png" | "webp"
    int Width,
    int Height,
    long FileSizeBytes
);
}
