namespace Account.Handler

#nowarn "20"

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain
open Account.Application

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()
        builder.Services.AddEndpointsApiExplorer()
        builder.Services.AddSwaggerGen()

        builder.Services.AddHandler(builder.Configuration)
        builder.Services.AddAggregate<Account>(builder.Configuration)
        builder.Services.AddSingleton<AccountService>()
        builder.Services.AddAggregate<Transaction>(builder.Configuration)
        builder.Services.AddSingleton<TransactionService>()

        builder.Services.AddKeyedSingleton<IStream, Stream<Account>>(typeof<Account>)
        |> ignore

        builder.Services.AddKeyedSingleton<IStream, Stream<Transaction>>(typeof<Transaction>)
        |> ignore

        let app = builder.Build()

        using (app.Services.CreateScope()) (fun scope ->
            let services = scope.ServiceProvider
            services.GetRequiredService<AccountService>()
            services.GetRequiredService<TransactionService>())

        if app.Environment.IsDevelopment() then
            app.UseSwagger()
            app.UseSwaggerUI()
            ()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
