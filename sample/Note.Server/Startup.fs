namespace Note.Server

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Note.Application


type Startup() =

    let es = Uri "tcp://admin:changeit@localhost:4011"
    let ld = Uri "tcp://admin:changeit@localhost:4012"
    let lg = Uri "tcp://admin:changeit@localhost:4013"

    member _.ConfigureServices(services: IServiceCollection) =
        services.AddGrpc() |> ignore
        services.AddSingleton (AppService(es, ld, lg)) |> ignore

    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if env.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore

        app.UseRouting() |> ignore

        app.UseEndpoints(fun endpoints ->
            endpoints.MapGrpcService<ActorService>() |> ignore
            endpoints.MapGrpcService<NoteService>() |> ignore

            endpoints.MapGet("/", fun context -> context.Response.WriteAsync("Hello World!")) |> ignore
            ) |> ignore