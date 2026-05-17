using MMS.Application.DTOs.Submission;
using MMS.Application.Interfaces.Services;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class SubmissionQueries
{
    /// <summary>Xem danh sách submission của một Mangaka</summary>
    public async Task<IEnumerable<SubmissionResponse>> GetMySubmissions(
        [Service] ISubmissionService submissionService,
        Guid mangakaId)
    {
        return await submissionService.GetByMangakaAsync(mangakaId);
    }

    /// <summary>Xem chi tiết một submission kèm bản thảo và bình chọn</summary>
    public async Task<SubmissionResponse?> GetSubmissionById(
        [Service] ISubmissionService submissionService,
        Guid id)
    {
        return await submissionService.GetByIdAsync(id);
    }
}
