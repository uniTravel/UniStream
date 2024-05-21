namespace Account.Initializer

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Confluent.Kafka.Admin
open UniStream.Domain
open Account.Domain


module Program =

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder(args)
        builder.Services.AddInitializer(builder.Configuration)

        use host = builder.Build()
        use serviceScope = host.Services.CreateScope()
        let services = serviceScope.ServiceProvider
        use admin = services.GetRequiredService<IAdmin>().Client

        let ta =
            [ typeof<Account>; typeof<Transaction> ]
            |> List.collect (fun t ->
                [ TopicSpecification(Name = t.FullName, ReplicationFactor = 1s, NumPartitions = 1)
                  TopicSpecification(Name = t.FullName + "_Post", ReplicationFactor = 1s, NumPartitions = 1)
                  TopicSpecification(Name = t.FullName + "_Reply", ReplicationFactor = 1s, NumPartitions = 1) ])

        admin.CreateTopicsAsync(ta).Wait()

        0 // exit code
