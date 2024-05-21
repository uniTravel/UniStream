[<AutoOpen>]
module Account.Domain.App

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain

let host =
    let builder = Host.CreateApplicationBuilder()
    builder.Services.AddSender(builder.Configuration)
    builder.Services.AddSingleton<Sender<Transaction>>() |> ignore
    builder.Build()
