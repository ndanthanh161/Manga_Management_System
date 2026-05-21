using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Infrastructure.gRPC
{
    /// <summary>
    /// Interceptor phía CLIENT (API) — tự inject service token vào metadata 
    /// trước mỗi gRPC call tới service nội bộ.
    /// </summary>
    public class GrpcAuthClientInterceptor : Interceptor
    {
        private readonly IConfiguration _config;
        public GrpcAuthClientInterceptor(IConfiguration config)
        {
            _config = config;
        }
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var headers = context.Options.Headers ?? new Metadata();
            headers.Add("authorization", $"Bearer {GetServiceToken()}");
            var newOptions = context.Options.WithHeaders(headers);
            var newContext = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, newOptions);
            return continuation(request, newContext);
        }
        private string GetServiceToken()
        {
            // Service token lấy từ config — long-lived, không phải user token
            return _config["Grpc:ServiceToken"]
                ?? throw new InvalidOperationException("Grpc:ServiceToken not configured");
        }
    }
}
