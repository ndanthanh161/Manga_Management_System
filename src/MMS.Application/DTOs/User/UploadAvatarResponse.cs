using MMS.Application.DTOs.ImageFile;

namespace MMS.Application.DTOs.User;

public record UploadAvatarResponse(
    UploadedImageDto Upload,
    ImageUrlsDto Urls
);
