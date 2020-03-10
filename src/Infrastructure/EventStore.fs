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

    let write (client: IEventStoreConnection) aggType aggId version eData =
        let streamName = aggType + "-" + aggId
        let version = version - 1L
        let eventData = eData |> Array.map (fun (evType, evData) -> EventData (Guid.NewGuid(), evType, true, evData, [||]))
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

    let write (client: IEventStoreConnection) cvType traceId aggId cData =
        let eventData = EventData (traceId, aggId, true, cData, [||])
        client.AppendToStreamAsync (cvType, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.RunSynchronously |> ignore

    let connectSubscription (client: IEventStoreConnection) cvType groupName f =
        Helper.connectSubscription client cvType groupName f


module DomainLog =

    let write (client: IEventStoreConnection) cvType status dLog =
        let eventData = EventData (Guid.NewGuid(), status, true, dLog, [||])
        client.AppendToStreamAsync (cvType, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.Ignore |> ignore


module DiagnoseLog =

    let write stream (client: IEventStoreConnection) aggType gLog =
        let eventData = EventData (Guid.NewGuid(), aggType, true, gLog, [||])
        client.AppendToStreamAsync (stream, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.Ignore |> ignore