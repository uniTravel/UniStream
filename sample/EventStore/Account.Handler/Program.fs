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
