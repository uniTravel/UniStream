[<AutoOpen>]
module Account.Domain.App

open Microsoft.Extensions.Hosting
open UniStream.Domain

let host =
    let builder = Host.CreateApplicationBuilder()
    builder.Services.AddSender(builder.Configuration)
    builder.Build()
