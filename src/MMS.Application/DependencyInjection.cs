using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MMS.Application.Interfaces.Services;
using MMS.Application.Services;

namespace MMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        // Mangaka Flow Services
        services.AddScoped<ISeriesService, SeriesService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IRegionService, RegionService>();
        services.AddScoped<IAnnotationService, AnnotationService>();
        services.AddScoped<IChapterService, ChapterService>();
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<IManuscriptService, ManuscriptService>();
        services.AddScoped<ICandidateApplicationService, CandidateApplicationService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
