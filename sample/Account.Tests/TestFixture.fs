[<AutoOpen>]
module Account.TestFixture.Common

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open UniStream.Domain
open Account.Domain
open Account.Application


let provider =
    let builder = Host.CreateApplicationBuilder()

    builder.Services
        .AddOptions<KurrentOptions>()
        .Bind(builder.Configuration.GetSection KurrentOptions.Name)
        .ValidateDataAnnotations()
        .ValidateOnStart()
    |> ignore

    builder.Services.AddSingleton<ISettings, Settings>().AddSingleton<IClient, Client>()
    |> ignore

    builder.Services
        .AddAggregate<Account>(builder.Configuration)
        .AddSingleton<IStream<Account>, Stream<Account>>()
        .AddSingleton<AccountService>()
    |> ignore

    builder.Services
        .AddAggregate<Transaction>(builder.Configuration)
        .AddSingleton<IStream<Transaction>, Stream<Transaction>>()
        .AddSingleton<TransactionService>()
    |> ignore

    let host = builder.Build()
    let scope = host.Services.CreateScope()

    scope.ServiceProvider
