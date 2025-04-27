namespace Account.Initializer

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Options
open Confluent.Kafka
open Confluent.Kafka.Admin
open UniStream.Domain
open Account.Domain


module Program =

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder args

        builder.Services.AddInitializer builder.Configuration |> ignore

        use host = builder.Build()
        use serviceScope = host.Services.CreateScope()
        let services = serviceScope.ServiceProvider
        let options = services.GetRequiredService<IOptions<AdminClientConfig>>()
        let cfg = options.Value
        use admin = AdminClientBuilder(cfg).Build()

        let ta =
            [ typeof<Account>; typeof<Transaction> ]
            |> List.collect (fun t ->
                [ TopicSpecification(Name = t.FullName, NumPartitions = 3)
                  TopicSpecification(Name = t.FullName + "_Command", NumPartitions = 3) ])

        try
            admin.CreateTopicsAsync(ta).Wait()
        with _ ->
            ()

        0 // exit code
