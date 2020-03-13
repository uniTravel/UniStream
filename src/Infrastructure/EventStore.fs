namespace UniStream.Infrastructure

open System
open System.Threading.Tasks
open EventStore.ClientAPI


module Helper =

    let connectSubscription (client: IEventStoreConnection) (streamName: string) (groupName: string) (f: Guid -> string -> byte[] -> Async<unit>) =
        client.ConnectToPersistentSubscription (streamName, groupName, (fun sub e ->
            f e.Event.EventId e.Event.EventType e.Event.Data |> Async.StartAsTask :> Task
        )) |> ignore


module DomainEvent =

    let get (client: IEventStoreConnection) aggType aggId version =
        let streamName = aggType + "-" + aggId
        let rec read events pos =
            let slice = (client.ReadStreamEventsForwardAsync (streamName, pos, 64, false)).Result
            let events = Array.append events slice.Events
            if slice.IsEndOfStream then events, slice.LastEventNumber
            else read events slice.NextEventNumber
        let events, version = read [||] version
        events |> Array.map (fun e -> (e.Event.EventId, e.Event.EventType, e.Event.Data)), version

    let write (client: IEventStoreConnection) aggType aggId version data metadate =
        let streamName = aggType + "-" + aggId
        let version = version - 1L
        let eventData = data |> Array.map (fun (evType, evData) -> EventData (Guid.NewGuid(), evType, true, evData, metadate))
        let result = client.AppendToStreamAsync (streamName, version, eventData) |> Async.AwaitTask |> Async.RunSynchronously
        result.NextExpectedVersion

    let subscribeToStream (client: IEventStoreConnection) evType (f: Guid -> string -> byte[] -> Async<unit>) =
        let streamName = "$et" + evType
        client.SubscribeToStreamAsync (streamName, true, (fun sub e ->
            f e.Event.EventId e.Event.EventType e.Event.Data |> Async.StartAsTask :> Task
        )) |> ignore

    let connectSubscription (client: IEventStoreConnection) evType groupName f =
        let streamName = "$et-" + evType
        Helper.connectSubscription client streamName groupName f


module DomainCommand =

    let write (client: IEventStoreConnection) cvType traceId aggId data =
        let eventData = EventData (traceId, aggId, true, data, [||])
        client.AppendToStreamAsync (cvType, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.RunSynchronously |> ignore

    let connectSubscription (client: IEventStoreConnection) cvType groupName f =
        Helper.connectSubscription client cvType groupName f


module DomainLog =

    let write ctx (client: IEventStoreConnection) user cvType data metadata =
        let streamName = ctx + "-" + user
        let eventData = EventData (Guid.NewGuid(), cvType, true, data, metadata)
        client.AppendToStreamAsync (streamName, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.Ignore |> ignore


module DiagnoseLog =

    let write ctx (client: IEventStoreConnection) aggType data =
        let eventData = EventData (Guid.NewGuid(), aggType, true, data, [||])
        client.AppendToStreamAsync (ctx, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.Ignore |> ignore