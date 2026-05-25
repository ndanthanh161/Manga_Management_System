using MMS.Application.DTOs.Annotation;
using MMS.Application.Interfaces.Services;

namespace MMS.API.GraphQL;

[ExtendObjectType(typeof(UserQueries))]
public class AnnotationQueries
{
    /// <summary>Xem các annotations (ghi chú phê duyệt) trên một trang</summary>
    public async Task<IEnumerable<AnnotationResponse>> GetAnnotationsByPage(
        [Service] IAnnotationService annotationService,
        Guid pageId)
    {
        var result = await annotationService.GetByPageAsync(pageId);
        return result.Data!;
    }

    /// <summary>Lấy số lượng annotation chưa được resolve cho từng trang trong chapter</summary>
    public async Task<IEnumerable<UnresolvedCountResponse>> GetUnresolvedCountByChapter(
        [Service] IAnnotationService annotationService,
        Guid chapterId)
    {
        var result = await annotationService.GetUnresolvedCountByChapterAsync(chapterId);
        return result.Data!;
    }
}
