using MMS.Application.DTOs.Region;
using MMS.Application.Interfaces.Services;
using MMS.Application.Interfaces;
using System.Security.Claims;
using HotChocolate.Authorization;
using MMS.API.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class RegionMutations
{
    [Authorize(Policy = PolicyNames.IsMangaka)]
    public async Task<RegionResponse> CreateRegion(
        [Service] IRegionService regionService,
        [Service] IUnitOfWork uow,
        ClaimsPrincipal claimsPrincipal,
        CreateRegionRequest input)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);

        var page = await uow.Pages.GetByIdAsync(input.PageId);
        if (page == null)
        {
            throw new System.Exception("Trang truyện không tồn tại.");
        }

        var chapter = await uow.Chapters.GetByIdAsync(page.ChapterId);
        if (chapter == null)
        {
            throw new System.Exception("Chương truyện không tồn tại.");
        }

        var series = await uow.Series.GetByIdAsync(chapter.SeriesId);
        if (series == null)
        {
            throw new System.Exception("Bộ truyện không tồn tại.");
        }

        if (series.AuthorId != mangakaId)
        {
            throw new System.Exception("Bạn không có quyền thao tác trên bộ truyện này.");
        }

        return await regionService.CreateAsync(mangakaId, input);
    }
}
