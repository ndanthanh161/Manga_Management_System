using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
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
            options.AddPolicy(PolicyNames.CanReviewSubmission, policy =>
                policy.RequireRole("Tantou Editor", "Editorial Board")
                      .AddRequirements(new TantouAssignmentRequirement()));

            options.AddPolicy(PolicyNames.CanManageSeries, policy =>
                policy.RequireRole("Mangaka", "Tantou Editor", "Editorial Board")
                      .AddRequirements(new SeriesOwnershipRequirement()));

            options.AddPolicy(PolicyNames.CanVoteInSession, policy =>
                policy.RequireRole("Editorial Board")
                      .AddRequirements(new BoardMemberRequirement()));

            // 1. Mangaka (Tác giả) Policies
            options.AddPolicy(PolicyNames.IsMangaka, policy =>
                policy.RequireRole("Mangaka")); // Tác giả — tạo series, nộp bản thảo, chọn vùng, tạo chapter/page

            options.AddPolicy(PolicyNames.CanAssignAssistantTasks, policy =>
                policy.RequireRole("Mangaka")
                      .AddRequirements(new SeriesOwnershipRequirement())); // Tác giả sở hữu series truyện

            options.AddPolicy(PolicyNames.CanReviewAssistantTasks, policy =>
                policy.RequireRole("Mangaka")
                      .AddRequirements(new TaskOwnerRequirement())); // Tác giả sở hữu series chứa task

            options.AddPolicy(PolicyNames.CanViewMySeriesMetrics, policy =>
                policy.RequireRole("Mangaka", "Tantou Editor", "Editorial Board")
                      .AddRequirements(new SeriesOwnershipRequirement())); // Chủ truyện, BTV phụ trách hoặc Hội đồng

            // 2. Assistant (Trợ lý) Policies
            options.AddPolicy(PolicyNames.CanAccessAssignedTask, policy =>
                policy.RequireRole("Assistant")
                      .AddRequirements(new TaskAssigneeRequirement())); // Trợ lý được giao nhiệm vụ

            options.AddPolicy(PolicyNames.CanSubmitTaskCompletion, policy =>
                policy.RequireRole("Assistant")
                      .AddRequirements(new TaskAssigneeRequirement())); // Trợ lý được giao nhiệm vụ nộp bài

            options.AddPolicy(PolicyNames.CanViewPersonalEarnings, policy =>
                policy.RequireRole("Assistant")
                      .AddRequirements(new SelfOwnershipRequirement())); // Chỉ xem thu nhập cá nhân của mình

            // 3. Tantou Editor (Biên tập viên) Policies
            options.AddPolicy(PolicyNames.CanAnnotateManuscript, policy =>
                policy.RequireRole("Tantou Editor")
                      .AddRequirements(new TantouAssignmentRequirement())); // Đúng BTV phụ trách bộ truyện

            options.AddPolicy(PolicyNames.CanManageSeriesMetrics, policy =>
                policy.RequireRole("Tantou Editor")
                      .AddRequirements(new TantouAssignmentRequirement())); // Đúng BTV phụ trách quản lý số liệu

            options.AddPolicy(PolicyNames.CanTrackStudioRealtime, policy =>
                policy.RequireRole("Tantou Editor")
                      .AddRequirements(new TantouAssignmentRequirement())); // Đúng BTV phụ trách theo dõi tiến độ

            // 4. Editorial Board (Hội đồng Biên tập) Policies
            options.AddPolicy(PolicyNames.CanInputReaderVotes, policy =>
                policy.RequireRole("Editorial Board", "Admin")); // Hội đồng biên tập hoặc Admin nhập điểm vote

            // 5. Annotation (dùng chung Tantou/Mangaka/Board)
            options.AddPolicy(PolicyNames.CanAnnotateChapter, policy =>
                policy.RequireRole("Tantou Editor", "Mangaka", "Editorial Board"));

            // 6. Task (dùng chung Mangaka + Assistant)
            options.AddPolicy(PolicyNames.CanUpdateTaskStatus, policy =>
                policy.RequireRole("Mangaka", "Assistant")); // Mangaka phê duyệt, Assistant hoàn thành

        });

        services.AddScoped<IAuthorizationHandler, TantouAssignmentHandler>();
        services.AddScoped<IAuthorizationHandler, BoardMemberHandler>();
        services.AddScoped<IAuthorizationHandler, SeriesOwnershipHandler>();
        services.AddScoped<IAuthorizationHandler, TaskAssigneeHandler>();
        services.AddScoped<IAuthorizationHandler, TaskOwnerHandler>();
        services.AddScoped<IAuthorizationHandler, SelfOwnershipHandler>();
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
            .AddTypeExtension<CandidateApplicationQueries>()
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
            .AddTypeExtension<CandidateApplicationMutations>()
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .AddAuthorization()
            .AddErrorFilter<GraphQLErrorFilter>();
    }
}
