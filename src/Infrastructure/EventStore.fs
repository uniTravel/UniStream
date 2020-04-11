namespace UniStream.Infrastructure

open System
open System.Threading.Tasks
open EventStore.ClientAPI


module DomainEvent =

    let get (client: IEventStoreConnection) prefix id version =
        let streamName = prefix + id
        let rec read events pos =
            let slice = (client.ReadStreamEventsForwardAsync (streamName, pos, 64, false)).Result
            let events = Array.append events slice.Events
            if slice.IsEndOfStream then events, slice.LastEventNumber
            else read events slice.NextEventNumber
        let events, version = read [||] version
        events |> Array.map (fun e -> (e.Event.EventType, e.Event.Data)), version

    let write (client: IEventStoreConnection) aggType aggId version eData = async {
        let streamName = aggType + aggId
        let version = version - 1L
        let eventData = eData |> Seq.map (fun (evType, evData, metadata) -> EventData (Guid.NewGuid(), evType, true, evData, metadata))
        let! result = client.AppendToStreamAsync (streamName, version, eventData) |> Async.AwaitTask
        return result.NextExpectedVersion
    }

    let subscribe (client: IEventStoreConnection) (prefix: string) (id: string)
        (handler: string -> string -> int64 -> byte[] -> byte[] -> Async<unit>)
        (dropHandler: string -> exn -> Async<unit>) =
        let streamName = prefix + id
        let sub =
            client.SubscribeToStreamAsync (streamName, true,
                (fun sub e ->
                    handler id e.Event.EventType e.Event.EventNumber e.Event.Data e.Event.Metadata |> Async.StartAsTask :> Task
                ),
                (fun sub (reason: SubscriptionDropReason) ex ->
                    let r = Enum.GetName (typeof<SubscriptionDropReason>, reason)
                    dropHandler r ex |> Async.Start
                    sub.Close()
                )
            ) |> Async.AwaitTask |> Async.RunSynchronously
        fun () -> sub.Close()

    let streamSubscribe (client: IEventStoreConnection) (streamName: string)
        (handler: string -> string -> int64 -> byte[] -> byte[] -> Async<unit>)
        (dropHandler: string -> exn -> Async<unit>) =
        let sub =
            client.SubscribeToStreamAsync (streamName, true,
                (fun sub e ->
                    let s = e.Event.EventStreamId
                    let idx = s.IndexOf '-' + 1
                    let aggId = s.[idx..s.Length]
                    handler aggId e.Event.EventType e.Event.EventNumber e.Event.Data e.Event.Metadata |> Async.StartAsTask :> Task
                ),
                (fun sub reason ex ->
                    let r = Enum.GetName (typeof<SubscriptionDropReason>, reason)
                    dropHandler r ex |> Async.Start
                    sub.Close()
                )
            ) |> Async.AwaitTask |> Async.RunSynchronously
        fun () -> sub.Close()

    let connectSubscription (client: IEventStoreConnection) (streamName: string) (groupName: string)
        (handler: string -> string -> int64 -> byte[] -> byte[] -> Async<unit>) =
        client.ConnectToPersistentSubscription (streamName, groupName, (fun sub e ->
            let s = e.Event.EventStreamId
            let idx = s.IndexOf '-' + 1
            let aggId = s.[idx..s.Length]
            handler aggId e.Event.EventType e.Event.EventNumber e.Event.Data e.Event.Metadata |> Async.StartAsTask :> Task
        )) |> ignore


module DomainCommand =

    let write (client: IEventStoreConnection) cvType (aggId: Guid) data metadata =
        let eventData = EventData (Guid.NewGuid(), aggId.ToString(), true, data, metadata)
        client.AppendToStreamAsync (cvType, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.Ignore

    let connectSubscription (client: IEventStoreConnection) (cvType: string) (groupName: string)
        (handler: string -> byte[] -> byte[] -> Async<unit>) =
        client.ConnectToPersistentSubscription (cvType, groupName, (fun sub e ->
            let aggId = e.Event.EventType
            handler aggId e.Event.Data e.Event.Metadata |> Async.StartAsTask :> Task
        )) |> ignore


module DomainLog =

    let write ctx (client: IEventStoreConnection) user category data metadata =
        let streamName = ctx + user
        let eventData = EventData (Guid.NewGuid(), category, true, data, metadata)
        client.AppendToStreamAsync (streamName, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.Ignore |> ignore


module DiagnoseLog =

    let write ctx (client: IEventStoreConnection) aggType data =
        let eventData = EventData (Guid.NewGuid(), aggType, true, data, [||])
        client.AppendToStreamAsync (ctx, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.Ignore |> ignore