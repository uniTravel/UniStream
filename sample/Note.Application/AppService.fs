namespace Note.Application

open System
open UniStream.Domain
open UniStream.Infrastructure
open Note.Domain


[<Sealed>]
type AppService (es: Uri, ld: Uri, lg: Uri) =

    let c1 = DomainEvent.create es
    let c2 = DomainLog.create ld
    let c3 = DiagnoseLog.create lg "NoteApp"
    let get = DomainEvent.get c1
    let esFunc = DomainEvent.write c1
    let ldFunc = DomainLog.write c2
    let lgFunc = DiagnoseLog.write c3
    let cfg = Aggregator.config get esFunc ldFunc lgFunc

    let actor = Aggregator.create<Actor.T> cfg 3L
    let note = Aggregator.create<Note.T> cfg 3L

    member _.CreateActor delta =
        Async.StartAsTask <| CommandService.createActor actor delta

    member _.CreateNote delta =
        Async.StartAsTask <| CommandService.createNote note delta

    member _.ChangeNote delta =
        Async.StartAsTask <| CommandService.changeNote note delta