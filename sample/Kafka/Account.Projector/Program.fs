namespace Account.Projector

open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain


module Program =

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder args

        builder.Services.AddProjector builder.Configuration |> ignore

        builder.Services.AddProjector<Account> builder.Configuration |> ignore
        builder.Services.AddProjector<Transaction> builder.Configuration |> ignore

        builder.Build().Run()

        0 // exit code
