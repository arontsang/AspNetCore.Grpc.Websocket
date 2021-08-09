using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace ArtsTech.Grpc.WebSocket
{
    public partial class GrpcWebSocketMiddleware : IMiddleware
    {
        private const string GrpcWebSocketSubProtocol = "grpc-websockets";

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.WebSockets.IsWebSocketRequest
                && context.WebSockets.WebSocketRequestedProtocols.Contains(GrpcWebSocketSubProtocol))
            {
                return InvokeWebSocketGrpc(context, next);
            }

            return next(context);
        }

        private async Task InvokeWebSocketGrpc(HttpContext context, RequestDelegate next)
        {
            using var websocket = await context.WebSockets.AcceptWebSocketAsync(GrpcWebSocketSubProtocol);

            await using var requestBody = new RequestBodyFeature(websocket);
            await using var responseBody = new ResponseFeature(websocket);

            var features = new FeatureCollection();
            features.Set<IRequestBodyPipeFeature>(requestBody);
            features.Set<IHttpRequestLifetimeFeature>(requestBody);
            features.Set<IHttpResponseFeature>(responseBody);
            features.Set<IHttpResponseBodyFeature>(responseBody);
            features.Set<IHttpResponseTrailersFeature>(responseBody);
            features.Set<IHttpRequestFeature>(new HttpRequestFeature());

            features.Set(context.Features.Get<IServiceProvidersFeature>());

            var grpcContext = new DefaultHttpContext(features);


            foreach (var (key, values) in context.Request.Headers)
            {
                grpcContext.Request.Headers[key] = values;
            }

            grpcContext.Request.Protocol = "HTTP/2";
            grpcContext.Request.ContentType = "application/grpc+proto";
            grpcContext.Request.Method = "POST";
            grpcContext.Request.Path = context.Request.Path;

            try
            {
                await next(grpcContext);
            }
            finally
            {
                await responseBody.CompleteAsync();
            }
        }


    }
}