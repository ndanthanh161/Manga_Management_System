using Mapster;
using MMS.Application.DTOs.Annotation;
using MMS.Domain.Entities;

namespace MMS.Application.Mappings;

public class AnnotationMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Annotation -> AnnotationResponse
        config.NewConfig<Annotation, AnnotationResponse>()
            .Map(dest => dest.UserName, src => src.User != null ? src.User.Username : string.Empty)
            .Map(dest => dest.ResolvedByUserName, src => src.ResolvedByUser != null ? src.ResolvedByUser.Username : null)
            .Map(dest => dest.ClosedByUserName, src => src.ClosedByUser != null ? src.ClosedByUser.Username : null)
            .Map(dest => dest.Status, src => src.Status.ToString());

        // CreateAnnotationRequest -> Annotation
        config.NewConfig<CreateAnnotationRequest, Annotation>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.User)
            .Ignore(dest => dest.Page)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow);
    }
}
