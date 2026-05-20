using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MMS.Domain.Exceptions;

namespace MMS.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var statusCode = ex switch
            {
                DomainException => 422,
                UnauthorizedAccessException => 401,
                _ => 500
            };

            var errorCode = ex is DomainException de ? de.ErrorCode : 
                            (ex is InfrastructureException ie ? ie.ErrorCode : "INTERNAL_ERROR");
                            
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            
            _logger.LogError(ex, "Exception {ErrorCode} | TraceId: {TraceId}", errorCode, traceId);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            
            var problem = new ProblemDetails
            {
                Title = statusCode == 500 ? "An internal server error occurred." : "Request failed",
                Detail = statusCode == 500 ? null : ex.Message,
                Status = statusCode,
                Instance = context.Request.Path,
                Extensions =
                {
                    ["traceId"] = traceId,
                    ["errorCode"] = errorCode
                }
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
