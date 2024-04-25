namespace UniStream.Domain

open System
open System.Text
open EventStore.Client


[<Sealed>]
type Stream(client: IClient) =
    let write (traceId: Guid option) aggType (aggId: Guid) revision evtType (evtData: byte array) =
        let stream = aggType + "-" + aggId.ToString()

        let data =
            match traceId with
            | Some traceId ->
                let metadata =
                    $"{{\"$correlationId\":\"{traceId}\"}}"
                    |> Encoding.ASCII.GetBytes
                    |> ReadOnlyMemory
                    |> Nullable

                EventData(Uuid.NewUuid(), evtType, evtData, metadata)
            | None -> EventData(Uuid.NewUuid(), evtType, evtData)

        client.Client
            .AppendToStreamAsync(stream, StreamRevision revision, [ data ])
            .Wait()

    let read aggType (aggId: Guid) =
        let stream = aggType + "-" + aggId.ToString()

        let e =
            client.Client
                .ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
                .GetAsyncEnumerator()

        [ while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
              yield e.Current.Event.EventType, e.Current.Event.Data ]


    interface IStream with
        member _.Reader = read
        member _.Writer = write
