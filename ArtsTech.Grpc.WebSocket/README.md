# gRPC Web Socket Proxy

## Motivation
This package is for adding support for [Improbable-Eng grpc-web's](https://github.com/improbable-eng/grpc-web)
implementation of the WebSocket transport for gRPC.

This enables support for gRPC Client Side Streaming, on top of Server-Side Streaming.

This means that you can use fully Bi-Directional streaming rpc from JavaScript/Typescript on your
AspNetCore gRPC server.

## Usage

```c#
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {    
        services.AddGrpc();
        services.AddGrpcReflection();
        services.AddGrpcWebSocket();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseWebSockets();
        // .UseGrpcWebSocket() MUST be called after UseWebSockets
        app.UseGrpcWebSocket();
        // .UseGrpcWebSocket() MUST be called before UseRouting        

        app.UseRouting();
        app.UseGrpcWeb(new GrpcWebOptions{DefaultEnabled = true});

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller}/{action=Index}/{id?}");

            endpoints.MapGrpcService<WeatherForecastController>();
            endpoints.MapGrpcReflectionService();
        });
    }
}
```

See Improbable-Eng for configuration of Javascript Client.