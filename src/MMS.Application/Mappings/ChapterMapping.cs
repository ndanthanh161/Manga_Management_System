using Mapster;
using MMS.Application.DTOs.Chapter;
using MMS.Domain.Entities;

namespace MMS.Application.Mappings;

public class ChapterMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateChapterRequest, Chapter>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Series)
            .Ignore(dest => dest.Pages)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
            .Map(dest => dest.IsPublished, src => false);

        config.NewConfig<Chapter, ChapterResponse>();
    }
}
