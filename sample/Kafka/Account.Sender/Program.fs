namespace Account.Sender

#nowarn "20"

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()
        builder.Services.AddSender(builder.Configuration)
        builder.Services.AddSingleton<Sender<Transaction>>() |> ignore

        let app = builder.Build()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
