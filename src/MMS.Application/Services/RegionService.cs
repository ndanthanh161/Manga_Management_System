using Mapster;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;

namespace MMS.Application.Services;

public class RegionService : IRegionService
{
    private readonly IUnitOfWork _uow;

    public RegionService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<RegionResponse> CreateAsync(Guid mangakaId, CreateRegionRequest request)
    {
        var region = request.Adapt<Region>();
        region.Id = Guid.NewGuid();

        await _uow.Regions.AddAsync(region);
        await _uow.SaveChangesAsync();

        return region.Adapt<RegionResponse>();
    }

    public async Task<IEnumerable<RegionResponse>> GetByPageAsync(Guid pageId)
    {
        var regions = await _uow.Regions.GetByPageIdAsync(pageId);
        return regions.Adapt<IEnumerable<RegionResponse>>();
    }
}
