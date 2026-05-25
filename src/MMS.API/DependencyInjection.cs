using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MMS.API.Authorization;
using MMS.API.Authorization.Handlers;
using MMS.API.Authorization.Requirements;
using MMS.API.GraphQL;
using System.Text;

namespace MMS.API;

public static class DependencyInjection
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        configuration["Jwt:Secret"]
                            ?? throw new InvalidOperationException("JWT Secret is not configured!")))
                };
            });

        services.AddAuthorization(options =>
        {
            // Policy đơn giản: chỉ check role
            options.AddPolicy(PolicyNames.IsTantou, policy =>
                policy.RequireRole("Tantou Editor"));

            options.AddPolicy(PolicyNames.IsBoardMember, policy =>
                policy.RequireRole("Editorial Board"));
            // Policy phức tạp: check role + resource ownership
            // (dùng với IAuthorizationService.AuthorizeAsync(user, resource, policy))
            options.AddPolicy(PolicyNames.CanReviewSubmission, policy =>
                policy.AddRequirements(new TantouAssignmentRequirement()));

            options.AddPolicy(PolicyNames.CanManageSeries, policy =>
                policy.AddRequirements(new SeriesOwnershipRequirement()));

            options.AddPolicy(PolicyNames.CanVoteInSession, policy =>
                policy.AddRequirements(new BoardMemberRequirement()));
        });

        services.AddScoped<IAuthorizationHandler, TantouAssignmentHandler>();
        services.AddScoped<IAuthorizationHandler, BoardMemberHandler>();
        services.AddScoped<IAuthorizationHandler, SeriesOwnershipHandler>();
        services.AddHttpContextAccessor();

        return services;
    }

    public static IRequestExecutorBuilder AddGraphQL(this IServiceCollection services)
    {
        return services
            .AddGraphQLServer()
            .AddQueryType<UserQueries>()
            .AddTypeExtension<SeriesQueries>()
            .AddTypeExtension<SubmissionQueries>()
            .AddTypeExtension<RegionQueries>()
            .AddTypeExtension<TaskQueries>()
            .AddTypeExtension<AnnotationQueries>()
            .AddTypeExtension<ChapterQueries>()
            .AddTypeExtension<PageQueries>()
            .AddTypeExtension<ManuscriptQueries>()
            .AddMutationType()
            .AddTypeExtension<AuthMutations>()
            .AddTypeExtension<UserMutations>()
            .AddTypeExtension<SeriesMutations>()
            .AddTypeExtension<SubmissionMutations>()
            .AddTypeExtension<RegionMutations>()
            .AddTypeExtension<TaskMutations>()
            .AddTypeExtension<AnnotationMutations>()
            .AddTypeExtension<ChapterMutations>()
            // .AddTypeExtension<PageMutations>()
            .AddTypeExtension<ManuscriptMutations>()
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .AddAuthorization()
            .AddErrorFilter<GraphQLErrorFilter>();
    }
}
