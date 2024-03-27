[<AutoOpen>]
module Account.Domain.App

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain
open Account.Application

let host =
    let builder = Host.CreateApplicationBuilder()
    builder.Services.AddEventStore(builder.Configuration, true)
    builder.Services.AddAggregate<Account>(builder.Configuration)
    builder.Services.AddSingleton<AccountService>() |> ignore
    builder.Services.AddAggregate<Transaction>(builder.Configuration)
    builder.Services.AddSingleton<TransactionService>() |> ignore
    builder.Build()
