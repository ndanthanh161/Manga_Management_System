using MMS.API;
using MMS.API.Middleware;
using MMS.Application;
using MMS.Application.Common;
using MMS.Infrastructure;
using MMS.Infrastructure.Data;
using Mapster;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed Database
await DbInitializer.SeedAsync(app.Services);

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();
app.MapControllers();

app.Run();
