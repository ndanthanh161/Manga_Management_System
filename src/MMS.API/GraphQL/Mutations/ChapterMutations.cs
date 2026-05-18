using MMS.Application.DTOs.Chapter;
using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class ChapterMutations
{
    [Authorize]
    public async Task<ChapterResponse> CreateChapter(
        [Service] IChapterService chapterService,
        CreateChapterRequest input)
    {
        return await chapterService.CreateAsync(input);
    }
}
