using MMS.Application.DTOs.Chapter;
using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;
using System.Security.Claims;
using MMS.API.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class ChapterMutations
{
    [Authorize(Policy = PolicyNames.IsMangaka)]
    public async Task<ChapterResponse> CreateChapter(
        [Service] IChapterService chapterService,
        [Service] ISeriesService seriesService,
        ClaimsPrincipal claimsPrincipal,
        CreateChapterRequest input)
    {
        var mangakaId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        
        var series = await seriesService.GetByIdAsync(input.SeriesId);
        if (series == null)
        {
            throw new System.Exception("Series không tồn tại.");
        }
        if (series.AuthorId != mangakaId)
        {
            throw new System.Exception("Bạn không có quyền tạo chapter cho series này.");
        }

        return await chapterService.CreateAsync(input);
    }
}
