[<AutoOpen>]
module Account.Domain.App

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain
open Account.Application

let host =
    let builder = Host.CreateApplicationBuilder()
    builder.Services.AddHandler(builder.Configuration)
    builder.Services.AddAggregate<Account>(builder.Configuration)
    builder.Services.AddSingleton<AccountService>() |> ignore
    builder.Services.AddAggregate<Transaction>(builder.Configuration)
    builder.Services.AddSingleton<TransactionService>() |> ignore

    builder.Services.AddKeyedSingleton<IStream, Stream<Account>>(typeof<Account>)
    |> ignore

    builder.Services.AddKeyedSingleton<IStream, Stream<Transaction>>(typeof<Transaction>)
    |> ignore

    builder.Build()
