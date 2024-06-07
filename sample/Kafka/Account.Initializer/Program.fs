namespace Account.Initializer

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Confluent.Kafka.Admin
open UniStream.Domain
open Account.Domain


type Mode =
    | Single
    | Multi


module Program =

    let spec (mode: Mode) (tl: Type list) =
        match mode with
        | Single -> tl |> List.map (fun t -> TopicSpecification(Name = t.FullName))
        | Multi ->
            tl
            |> List.collect (fun t ->
                [ TopicSpecification(Name = t.FullName, NumPartitions = 3)
                  TopicSpecification(Name = t.FullName + "_Post", NumPartitions = 3)
                  TopicSpecification(Name = t.FullName + "_Reply") ])

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder(args)
        builder.Services.AddInitializer(builder.Configuration)

        use host = builder.Build()
        use serviceScope = host.Services.CreateScope()
        let services = serviceScope.ServiceProvider
        use admin = services.GetRequiredService<IAdmin>().Client
        let ta = spec Multi <| [ typeof<Account>; typeof<Transaction> ]
        admin.CreateTopicsAsync(ta).Wait()

        0 // exit code
