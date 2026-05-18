using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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

        services.AddAuthorization();
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
            .AddMutationType()
            .AddTypeExtension<AuthMutations>()
            .AddTypeExtension<UserMutations>()
            .AddTypeExtension<SeriesMutations>()
            .AddTypeExtension<SubmissionMutations>()
            .AddTypeExtension<RegionMutations>()
            .AddTypeExtension<TaskMutations>()
            .AddTypeExtension<AnnotationMutations>()
            .AddTypeExtension<ChapterMutations>()
            .AddTypeExtension<PageMutations>()
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .AddAuthorization();
    }
}
