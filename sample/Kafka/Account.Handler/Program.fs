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

        let builder = WebApplication.CreateBuilder args

        builder.Services.AddControllers()
        builder.Services.AddOpenApi()
        builder.Services.AddHandler builder.Configuration

        builder.Services.AddHandler<Account>(builder.Configuration).AddSingleton<AccountService>()

        let app = builder.Build()

        using (app.Services.CreateScope()) (fun scope ->
            let services = scope.ServiceProvider
            services.GetRequiredService<AccountService>())

        if app.Environment.IsDevelopment() then
            app.MapOpenApi()
            app.UseSwaggerUI(fun options -> options.SwaggerEndpoint("/openapi/v1.json", "v1"))
            ()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
