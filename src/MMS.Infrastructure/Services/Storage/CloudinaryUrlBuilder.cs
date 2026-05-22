using MMS.Domain.Enums;

namespace MMS.Infrastructure.Services.Storage;

public static class CloudinaryUrlBuilder
{
    public static string Build(string cloudName, string publicId, ImageVariant variant)
    {
        var transformation = variant switch
        {
            ImageVariant.Thumbnail256 => "c_fill,w_256,h_256,f_auto,q_auto",
            ImageVariant.Medium512 => "c_limit,w_512,f_auto,q_auto",
            ImageVariant.Large1024 => "c_limit,w_1024,f_auto,q_auto",
            ImageVariant.Original => "",
            _ => ""
        };

        var transformPart = string.IsNullOrEmpty(transformation) ? "" : $"{transformation}/";
        return $"https://res.cloudinary.com/{cloudName}/image/upload/{transformPart}{publicId}";
    }
}
