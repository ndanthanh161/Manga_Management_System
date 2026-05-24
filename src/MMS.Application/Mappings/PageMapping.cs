using Mapster;
using MMS.Application.DTOs.ImageFile;
using MMS.Application.DTOs.Page;
using MMS.Domain.Entities;

namespace MMS.Application.Mappings;

public class PageMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UploadedImageDto, Page>()
            .Map(dest => dest.CloudinaryPublicId, src => src.PublicId)
            .Map(dest => dest.ImageUrl, src => src.SecureUrl)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.PageNumber)
            .Ignore(dest => dest.ChapterId)
            .Ignore(dest => dest.Chapter)
            .Ignore(dest => dest.Version)
            .Ignore(dest => dest.IsCurrentVersion)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.Annotations)
            .Ignore(dest => dest.Comments);

        config.NewConfig<Page, PageResponse>();
    }
}
