using MMS.Application.DTOs.Annotation;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class AnnotationMutations
{
    [Authorize(Roles = new[] { "Mangaka" })]
    public async Task<AnnotationResponse> CreateAnnotation(
        [Service] IAnnotationService annotationService,
        ClaimsPrincipal claimsPrincipal,
        CreateAnnotationRequest input)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await annotationService.CreateAsync(userId, input);
        if (!result.Succeeded)
        {
            throw new System.Exception(result.Message ?? "Tạo ghi chú thất bại.");
        }
        return result.Data!;
    }
}
