namespace UniStream.Infrastructure

open System
open System.Threading.Tasks
open EventStore.ClientAPI


module DomainEvent =

    let get (client: IEventStoreConnection) aggType (aggId: Guid) version =
        let streamName = aggType + "-" + aggId.ToString()
        let rec read events pos =
            let slice = (client.ReadStreamEventsForwardAsync (streamName, pos, 64, false)).Result
            let events = Array.append events slice.Events
            if slice.IsEndOfStream then events, slice.LastEventNumber
            else read events slice.NextEventNumber
        let events, version = read [||] version
        events |> Array.map (fun e -> (e.Event.EventId, e.Event.EventType, e.Event.Data)), version

    let write (client: IEventStoreConnection) aggType (aggId: Guid) version data metadate =
        let streamName = aggType + "-" + aggId.ToString()
        let version = version - 1L
        let eventData = data |> Array.map (fun (evType, evData) -> EventData (Guid.NewGuid(), evType, true, evData, metadate))
        let result = client.AppendToStreamAsync (streamName, version, eventData) |> Async.AwaitTask |> Async.RunSynchronously
        result.NextExpectedVersion

    let subscribeToStream (client: IEventStoreConnection) (streamName: string) (handler: Guid -> string -> int64 -> byte[] -> byte[] -> Async<unit>) =
        client.SubscribeToStreamAsync (streamName, true, (fun sub e ->
            let s = e.Event.EventStreamId
            let idx = s.IndexOf '-' + 1
            let aggId = Guid s.[idx..s.Length]
            handler aggId e.Event.EventType e.Event.EventNumber e.Event.Data e.Event.Metadata |> Async.StartAsTask :> Task
        )) |> ignore

    let connectSubscription (client: IEventStoreConnection) (streamName: string) (groupName: string) (handler: Guid -> string -> int64 -> byte[] -> byte[] -> Async<unit>) =
        client.ConnectToPersistentSubscription (streamName, groupName, (fun sub e ->
            let s = e.Event.EventStreamId
            let idx = s.IndexOf '-' + 1
            let aggId = Guid s.[idx..s.Length]
            handler aggId e.Event.EventType e.Event.EventNumber e.Event.Data e.Event.Metadata |> Async.StartAsTask :> Task
        )) |> ignore


module DomainCommand =

    let write (client: IEventStoreConnection) cvType (aggId: Guid) data metadata =
        let eventData = EventData (Guid.NewGuid(), aggId.ToString(), true, data, metadata)
        client.AppendToStreamAsync (cvType, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.RunSynchronously |> ignore

    let connectSubscription (client: IEventStoreConnection) (cvType: string) (groupName: string) (handler: Guid -> byte[] -> byte[] -> Async<unit>) =
        client.ConnectToPersistentSubscription (cvType, groupName, (fun sub e ->
            let aggId = Guid e.Event.EventType
            handler aggId e.Event.Data e.Event.Metadata |> Async.StartAsTask :> Task
        )) |> ignore


module DomainLog =

    let write ctx (client: IEventStoreConnection) user category data metadata =
        let streamName = ctx + "-" + user
        let eventData = EventData (Guid.NewGuid(), category, true, data, metadata)
        client.AppendToStreamAsync (streamName, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.Ignore |> ignore


module DiagnoseLog =

    let write ctx (client: IEventStoreConnection) aggType data =
        let eventData = EventData (Guid.NewGuid(), aggType, true, data, [||])
        client.AppendToStreamAsync (ctx, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.Ignore |> ignore