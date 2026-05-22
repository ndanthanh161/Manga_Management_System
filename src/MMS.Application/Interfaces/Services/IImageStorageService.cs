using MMS.Application.Common;
using MMS.Application.DTOs.ImageFile;
using MMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Application.Interfaces.Services
{
    public interface IImageStorageService
    {
        Task<Result<UploadedImageDto>> UploadImageAsync(
        Stream stream, string folder, string publicIdHint, CancellationToken ct = default);

        Task<Result<bool>> DeleteImageAsync(string publicId, CancellationToken ct = default);

        string GetTransformedUrl(string publicId, ImageVariant variant);

        ImageUrlsDto BuildAllUrls(string publicId);
    }
}
