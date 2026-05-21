using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MMS.Protos.ImageProcessing.V1;
using Polly;
using System;

namespace MMS.Infrastructure.gRPC;

public static class GrpcClientRegistrationExtensions
{
    public static IServiceCollection AddMmsGrpcClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection("Grpc").Get<GrpcClientOptions>()
            ?? new GrpcClientOptions();

        // ImageProcessor client (Phase 3 sẽ dùng)
        services.AddGrpcClient<ImageProcessor.ImageProcessorClient>(o =>
        {
            o.Address = new Uri(options.ImageProcessorEndpoint);
        })
        .ConfigureChannel(ch =>
        {
            ch.MaxReceiveMessageSize = options.MaxReceiveMessageSizeMb * 1024 * 1024;
        })
        .AddInterceptor<GrpcAuthClientInterceptor>()
        .AddTransientHttpErrorPolicy(p =>
            p.WaitAndRetryAsync(options.RetryCount,
                attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))));

        return services;
    }
}
