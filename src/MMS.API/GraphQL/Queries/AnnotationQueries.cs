using MMS.Application.DTOs.Annotation;
using MMS.Application.Interfaces.Services;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class AnnotationQueries
{
    /// <summary>Xem các annotations (ghi chú phê duyệt) trên một trang</summary>
    [Authorize]
    public async Task<IEnumerable<AnnotationResponse>> GetAnnotationsByPage(
        [Service] IAnnotationService annotationService,
        Guid pageId)
    {
        var result = await annotationService.GetByPageAsync(pageId);
        if (!result.Succeeded)
        {
            throw new System.Exception(result.Message ?? "Lấy danh sách ghi chú thất bại.");
        }
        return result.Data!;
    }

    /// <summary>Lấy số lượng annotation chưa được resolve cho từng trang trong chapter</summary>
    [Authorize]
    public async Task<IEnumerable<UnresolvedCountResponse>> GetUnresolvedCountByChapter(
        [Service] IAnnotationService annotationService,
        Guid chapterId)
    {
        var result = await annotationService.GetUnresolvedCountByChapterAsync(chapterId);
        return result.Data!;
    }
}
