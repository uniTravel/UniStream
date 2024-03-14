namespace Account.Api

#nowarn "20"

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open EventStore.Client
open UniStream.Infrastructure
open Account.Application


module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()
        builder.Services.AddEndpointsApiExplorer()
        builder.Services.AddSwaggerGen()

        let conn =
            "esdb://admin:changeit@127.0.0.1:2111,127.0.0.1:2112,127.0.0.1:2113?tls=true&tlsVerifyCert=false"

        let client = new EventStoreClient(EventStoreClientSettings.Create(conn))
        let es = Stream(client)
        builder.Services.AddSingleton(new AccountService(es.Write, es.Read, 10000, 0.2))

        let app = builder.Build()

        if app.Environment.IsDevelopment() then
            app.UseSwagger()
            app.UseSwaggerUI()
            ()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
