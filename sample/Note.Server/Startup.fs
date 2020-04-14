namespace Note.Server

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open EventStore.ClientAPI
open UniStream.Infrastructure
open Note.Application


type Startup() =

    let connect (uri: Uri) =
        let conn = EventStoreConnection.Create uri
        conn.ConnectAsync() |> Async.AwaitTask |> Async.RunSynchronously
        conn

    let esUri = Uri "tcp://admin:changeit@localhost:4011"
    let ldUri = Uri "tcp://admin:changeit@localhost:4012"
    let lgUri = Uri "tcp://admin:changeit@localhost:4013"

    let c1 = connect esUri
    let c2 = connect ldUri
    let c3 = connect lgUri

    let reader = DomainEvent.get c1
    let writer = DomainEvent.write c1
    let ld = DomainLog.write c2
    let lg = DiagnoseLog.write c3

    member _.ConfigureServices(services: IServiceCollection) =
        services.AddGrpc() |> ignore
        services.AddSingleton (AppService (reader, writer, ld, lg)) |> ignore

    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if env.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore

        app.UseRouting() |> ignore

        app.UseEndpoints(fun endpoints ->
            endpoints.MapGrpcService<ActorService>() |> ignore
            endpoints.MapGrpcService<NoteService>() |> ignore

            endpoints.MapGet("/", fun context -> context.Response.WriteAsync("Hello World!")) |> ignore
            ) |> ignore