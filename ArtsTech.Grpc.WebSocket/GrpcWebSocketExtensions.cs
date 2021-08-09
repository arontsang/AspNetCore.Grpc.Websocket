using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ArtsTech.Grpc.WebSocket
{
    public static class GrpcWebSocketExtensions
    {
        
        public static IServiceCollection AddGrpcWebSocket(this IServiceCollection services)
        {
            return services.AddSingleton<GrpcWebSocketMiddleware>();
        }

        public static IApplicationBuilder UseGrpcWebSocket(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GrpcWebSocketMiddleware>();
        }
    }
}