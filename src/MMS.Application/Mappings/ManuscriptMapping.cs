using Mapster;
using MMS.Application.DTOs.Manuscript;
using MMS.Domain.Entities;

namespace MMS.Application.Mappings;

public class ManuscriptMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Manuscript, ManuscriptResponse>();
    }
}
