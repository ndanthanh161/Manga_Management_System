using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MMS.API.GraphQL;
using MMS.API.Middleware;
using MMS.Application;
using MMS.Application.Common;
using MMS.Infrastructure;
using MMS.Infrastructure.Data;
using Mapster;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Domain Layers & Mapping
MappingConfig.Configure();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Authentication & Authorization (Registered directly)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured!")))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// 3. GraphQL Configuration (Registered directly)
builder.Services.AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType()
    .AddTypeExtension<AuthMutations>()
    .AddTypeExtension<UserMutations>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddAuthorization();
    // .AddErrorFilter<GraphQLErrorFilter>();

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
