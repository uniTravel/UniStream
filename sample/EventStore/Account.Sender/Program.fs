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
        builder.Services.AddCommand<Transaction>(builder.Configuration)

        builder.Services.AddKeyedSingleton<ISender, Sender<Transaction>>(typeof<Transaction>)
        |> ignore

        let app = builder.Build()

        using (app.Services.CreateScope()) (fun scope ->
            let services = scope.ServiceProvider
            services.GetRequiredKeyedService<ISender>(typeof<Transaction>))

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
