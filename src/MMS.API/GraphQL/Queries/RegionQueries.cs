using MMS.Application.DTOs.Region;
using MMS.Application.Interfaces.Services;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class RegionQueries
{
    /// <summary>Xem các vùng đã chọn trên một trang truyện</summary>
    public async Task<IEnumerable<RegionResponse>> GetRegionsByPage(
        [Service] IRegionService regionService,
        Guid pageId)
    {
        return await regionService.GetByPageAsync(pageId);
    }
}
