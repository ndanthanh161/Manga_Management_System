using Mapster;
using MMS.Application.DTOs.Page;
using MMS.Domain.Entities;

namespace MMS.Application.Mappings;

public class PageMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreatePageRequest, Page>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Chapter)
            .Ignore(dest => dest.Annotations)
            .Ignore(dest => dest.Comments);

        config.NewConfig<Page, PageResponse>();
    }
}
