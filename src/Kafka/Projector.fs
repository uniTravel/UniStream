namespace UniStream.Domain

open System
open System.Text
open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Confluent.Kafka
open UniStream.Domain


[<Sealed>]
type Projector<'agg when 'agg :> Aggregate>(logger: ILogger<Projector<'agg>>, ap: IProducer<'agg>, tc: IConsumer<'agg>)
    =
    inherit BackgroundService()
    let ap = ap.Client
    let tc = tc.Client
    let aggType = typeof<'agg>.FullName

    let isRecoverableError (err: Error) =
        match err.Code with
        | ErrorCode.LeaderNotAvailable
        | ErrorCode.NotLeaderForPartition
        | ErrorCode.RequestTimedOut
        | ErrorCode.BrokerNotAvailable
        | ErrorCode.ReplicaNotAvailable
        | ErrorCode.NetworkException
        | ErrorCode.GroupLoadInProgress
        | ErrorCode.GroupCoordinatorNotAvailable
        | ErrorCode.NotEnoughReplicas
        | ErrorCode.NotEnoughReplicasAfterAppend
        | ErrorCode.TransactionalIdAuthorizationFailed
        | ErrorCode.ClusterAuthorizationFailed
        | ErrorCode.Local_QueueFull
        | ErrorCode.Local_TimedOut -> true
        | _ -> false

    let calculateRetryDelay retryCount =
        let delayMs = int <| Math.Min(1000.0 * Math.Pow(2, retryCount), 60000)
        TimeSpan.FromMilliseconds delayMs

    let delivery (aggId: Guid) (evtType: string) (report: DeliveryReport<byte array, byte array>) =
        match report.Error.Code with
        | ErrorCode.NoError -> logger.LogInformation $"Project {evtType} of {aggType}[{aggId}] success"
        | err -> failwith <| err.GetReason()

    override _.ExecuteAsync(stoppingToken: CancellationToken) =
        task {
            let mutable shouldRun = true
            ap.InitTransactions(TimeSpan.FromSeconds 30.0)
            tc.Subscribe aggType
            logger.LogInformation $"Subscription for {aggType} started"
            do! Tasks.Task.Delay 10

            while shouldRun && not stoppingToken.IsCancellationRequested do
                try
                    match tc.Consume stoppingToken with
                    | null -> ()
                    | cr ->
                        let evtType = cr.Message.Headers.GetLastBytes "evtType"

                        match Encoding.ASCII.GetString evtType with
                        | "Fail"
                        | "Duplicate" -> ()
                        | _ ->
                            ap.BeginTransaction()
                            let aggId = Guid cr.Message.Key
                            let topic = aggType + "-" + aggId.ToString()
                            let msg = Message<byte array, byte array>(Key = evtType, Value = cr.Message.Value)
                            ap.Produce(topic, msg, delivery aggId (Encoding.ASCII.GetString evtType))
                            let offsets = [ TopicPartitionOffset(cr.TopicPartition, cr.Offset + 1) ]
                            ap.SendOffsetsToTransaction(offsets, tc.ConsumerGroupMetadata, TimeSpan.FromSeconds 10.0)
                            ap.CommitTransaction()
                with
                | :? KafkaException as ex when isRecoverableError ex.Error ->
                    ap.AbortTransaction()
                    logger.LogError(ex, "Recoverable errors")
                    Thread.Sleep(calculateRetryDelay 1)
                | ex ->
                    ap.AbortTransaction()
                    logger.LogError(ex, "Consume loop breaked")
                    shouldRun <- false
        }
