using Mapster;
using Microsoft.OpenApi;
using MMS.API;
using MMS.API.Middleware;
using MMS.Application;
using MMS.Application.Common;
using MMS.Infrastructure;
using MMS.Infrastructure.Data;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting MMS API");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.File("logs/mms-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    );

    // 1. Domain Layers & Mapping
    MappingConfig.Configure();
    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);

    // 2. Authentication & Authorization
    builder.Services.AddJwtAuthentication(builder.Configuration);

    // 3. GraphQL Configuration
    builder.Services.AddGraphQL();

    // 4. API Tools
    builder.Services.AddMapster();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowNextJs", policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MMS API",
            Version = "v1",
            Description = "Manga Management System API",
            Contact = new OpenApiContact
            {
                Name = "MMS Team"
            }
        });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using Bearer scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer"
        });

        options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer", null, null),
                new List<string>()
            }
        });
    });

    var app = builder.Build();

    // Seed Database
    await DbInitializer.SeedAsync(app.Services);

    // Pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString());
            diag.Set("ClientIP", ctx.Connection.RemoteIpAddress?.ToString());
        };
    });

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<TokenBlacklistMiddleware>();

    app.UseHttpsRedirection();
    app.UseCors("AllowNextJs");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGraphQL();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex.GetType().Name is not "HostAbortedException")
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
