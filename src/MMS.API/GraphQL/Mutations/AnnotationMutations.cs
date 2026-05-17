using MMS.Application.DTOs.Annotation;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class AnnotationMutations
{
    [Authorize]
    public async Task<AnnotationResponse> CreateAnnotation(
        [Service] IAnnotationService annotationService,
        ClaimsPrincipal claimsPrincipal,
        CreateAnnotationRequest input)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        return await annotationService.CreateAsync(userId, input);
    }
}
