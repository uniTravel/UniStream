namespace Account.TestFixture

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open UniStream.Domain
open Account.Domain
open Account.Application


[<AutoOpen>]
module Host =
    let provider =
        let builder = Host.CreateApplicationBuilder()
        builder.Services.AddHandler(builder.Configuration) |> ignore

        builder.Services.AddHandler<Account>(builder.Configuration).AddSingleton<AccountService>()
        |> ignore

        builder.Services.AddHandler<Transaction>(builder.Configuration).AddSingleton<TransactionService>()
        |> ignore

        let host = builder.Build()
        let scope = host.Services.CreateScope()

        scope.ServiceProvider
