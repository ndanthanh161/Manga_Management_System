using Mapster;
using MMS.Domain.Entities;

namespace MMS.Application.Mappings;

public class AnnotationMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Annotation -> AnnotationResponse
        config.NewConfig<Annotation, AnnotationResponse>()
            .Map(dest => dest.UserName, src => src.User != null ? src.User.Username : string.Empty);

        // CreateAnnotationRequest -> Annotation
        config.NewConfig<CreateAnnotationRequest, Annotation>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.User)
            .Ignore(dest => dest.Page)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow);
    }
}
