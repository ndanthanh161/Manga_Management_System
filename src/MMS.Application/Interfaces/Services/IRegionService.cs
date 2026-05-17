namespace MMS.Application.Interfaces.Services;

public interface IRegionService
{
    Task<RegionResponse> CreateAsync(Guid mangakaId, CreateRegionRequest request);
    Task<IEnumerable<RegionResponse>> GetByPageAsync(Guid pageId);
}
