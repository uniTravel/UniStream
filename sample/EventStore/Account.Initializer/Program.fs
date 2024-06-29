namespace Account.Initializer

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain

module Program =

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder(args)
        builder.Services.AddInitializer(builder.Configuration) |> ignore

        use host = builder.Build()
        use serviceScope = host.Services.CreateScope()
        let services = serviceScope.ServiceProvider
        use manager = services.GetRequiredService<IManager>().Manager
        manager.EnableAsync("$by_category").Wait()

        0 // exit code
