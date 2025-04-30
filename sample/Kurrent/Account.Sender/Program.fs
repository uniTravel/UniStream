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
        builder.Services.AddSender builder.Configuration

        builder.Services.AddSender<Account> builder.Configuration
        builder.Services.AddSender<Transaction> builder.Configuration

        let app = builder.Build()

        using (app.Services.CreateScope()) (fun scope ->
            let services = scope.ServiceProvider
            services.GetRequiredService<ISender<Account>>()
            services.GetRequiredService<ISender<Transaction>>())

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
