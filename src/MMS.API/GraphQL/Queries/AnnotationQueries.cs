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
        if (!result.Succeeded)
        {
            throw new System.Exception(result.Message ?? "Lấy danh sách ghi chú thất bại.");
        }
        return result.Data!;
    }
}
