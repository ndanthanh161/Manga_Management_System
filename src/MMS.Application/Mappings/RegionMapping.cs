using Mapster;
using MMS.Domain.Entities;

namespace MMS.Application.Mappings;

public class RegionMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Region -> RegionResponse (field names match, Mapster tự map)
        config.NewConfig<Region, RegionResponse>();

        // CreateRegionRequest -> Region
        config.NewConfig<CreateRegionRequest, Region>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Page!)
            .Ignore(dest => dest.Tasks);
    }
}
