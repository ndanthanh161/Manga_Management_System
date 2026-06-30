using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Infrastructure.gRPC
{
    public class GrpcAuthServerInterceptor : Interceptor
    {
        private readonly IConfiguration _config;
        public GrpcAuthServerInterceptor(IConfiguration config)
        {
            _config = config;
        }
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            ValidateToken(context);
            return await continuation(request, context);
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            ValidateToken(context);
            await continuation(request, responseStream, context);
        }
        private void ValidateToken(ServerCallContext context)
        {
            var authHeader = context.RequestHeaders
                .FirstOrDefault(h => h.Key == "authorization")?.Value;
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing token"));
            }
            var token = authHeader["Bearer ".Length..];
            var key = _config["Grpc:ServiceTokenSecret"]
                ?? throw new InvalidOperationException("Grpc:ServiceTokenSecret not configured");
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            }, out _);
            // Token hợp lệ — có thể extract claims vào UserState nếu cần
            context.UserState["Authenticated"] = true;
        }
    }
}
