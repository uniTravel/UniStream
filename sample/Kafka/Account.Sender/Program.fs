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

        let builder = WebApplication.CreateBuilder args

        builder.Services.AddControllers()
        builder.Services.AddOpenApi()
        builder.Services.AddSender builder.Configuration

        builder.Services.AddSender<Account> builder.Configuration
        builder.Services.AddSender<Transaction> builder.Configuration

        let app = builder.Build()

        app.MapOpenApi()
        app.UseSwaggerUI(fun options -> options.SwaggerEndpoint("/openapi/v1.json", "v1"))

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
