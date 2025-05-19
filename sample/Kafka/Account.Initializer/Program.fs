namespace Account.Initializer

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Options
open Confluent.Kafka
open Confluent.Kafka.Admin
open UniStream.Domain
open Account.Domain


module Program =

    let getEnv (key: string) =
        match Environment.GetEnvironmentVariable key with
        | null -> None
        | value -> Some value

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder args

        builder.Services.AddInitializer builder.Configuration |> ignore

        use host = builder.Build()
        use serviceScope = host.Services.CreateScope()
        let services = serviceScope.ServiceProvider
        let options = services.GetRequiredService<IOptions<AdminClientConfig>>()
        let cfg = options.Value
        let partitions = getEnv "PARTITIONS" |> Option.defaultValue "3"
        use admin = AdminClientBuilder(cfg).Build()

        let ta =
            [ typeof<Account>; typeof<Transaction> ]
            |> List.collect (fun t ->
                [ TopicSpecification(Name = t.FullName, NumPartitions = int partitions)
                  TopicSpecification(Name = t.FullName + "_Command", NumPartitions = int partitions) ])

        try
            admin.CreateTopicsAsync(ta).Wait()
        with _ ->
            ()

        0 // exit code
