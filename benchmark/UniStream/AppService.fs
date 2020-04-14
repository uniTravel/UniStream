namespace Benchmark.UniStream

open System
open EventStore.ClientAPI
open UniStream.Domain
open UniStream.Infrastructure


[<Sealed>]
type AppService (es: Uri, ld: Uri, lg: Uri) =

    let connect (uri: Uri) =
        let conn = EventStoreConnection.Create uri
        conn.ConnectAsync() |> Async.AwaitTask |> Async.RunSynchronously
        conn

    let c1 = connect es
    let c2 = connect ld
    let c3 = connect lg
    let get = DomainEvent.get c1
    let esFunc = DomainEvent.write c1
    let ldFunc = DomainLog.write c2 "NoteApp"
    let lgFunc = DiagnoseLog.write c3 "NoteApp"

    let note = Mutable.create <| Config.Mutable (get false, esFunc, ldFunc, lgFunc)

    member _.CreateNote user aggId traceId cv =
        let command = CreateNote.create cv
        Mutable.apply note user aggId traceId command

    member _.ChangeNote user aggId traceId cv =
        let command = ChangeNote.create cv
        Mutable.apply note user aggId traceId command

    member _.BatchChangeNote user aggId traceId cv =
        let command = ChangeNote.create cv
        Mutable.batchApply note user aggId traceId command

    member _.GetNote aggId =
        Mutable.get note aggId