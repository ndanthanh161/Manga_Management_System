using MMS.Application.DTOs.Annotation;
using MMS.Application.Interfaces.Services;
using System.Security.Claims;
using HotChocolate.Authorization;

namespace MMS.API.GraphQL;

[ExtendObjectType("Mutation")]
public class AnnotationMutations
{
    [Authorize(Roles = new[] { "Tantou Editor", "Mangaka", "Editorial Board" })]
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

    [Authorize(Roles = new[] { "Mangaka", "Assistant" })]
    public async Task<AnnotationResponse> ResolveAnnotation(
        [Service] IAnnotationService annotationService,
        ClaimsPrincipal claimsPrincipal,
        Guid annotationId)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await annotationService.ResolveAsync(userId, annotationId);
        return result.Data!;
    }

    [Authorize(Roles = new[] { "Tantou Editor", "Editorial Board" })]
    public async Task<AnnotationResponse> ReviewAnnotation(
        [Service] IAnnotationService annotationService,
        ClaimsPrincipal claimsPrincipal,
        Guid annotationId,
        ReviewAnnotationRequest request)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await annotationService.ReviewAsync(userId, annotationId, request);
        return result.Data!;
    }

    [Authorize]
    public async Task<bool> DeleteAnnotation(
        [Service] IAnnotationService annotationService,
        ClaimsPrincipal claimsPrincipal,
        Guid annotationId)
    {
        var userId = GraphQLHelper.GetCurrentUserId(claimsPrincipal);
        var result = await annotationService.DeleteAsync(userId, annotationId);
        return result.Data;
    }
}
