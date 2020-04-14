namespace Note.Application

open UniStream.Domain
open UniStream.Infrastructure
open Note.Domain


[<Sealed>]
type AppService (reader: EventReader, writer: EventWriter, ld: DomainLogger, lg: DiagnoseLogger) =

    let actor = Immutable.create <| Config.Immutable (writer, ld "NoteApp", lg "NoteApp")
    let note = Mutable.create <| Config.Mutable (reader false, writer, ld "NoteApp", lg "NoteApp")
    let mutable noteObserver : Observer.T<NoteObserver.T> list = []

    member _.AddNoteObserver (sub: Subscriber) : unit =
        noteObserver <- (Observer.create<NoteObserver.T> <| Config.Observer (reader true, lg "NoteApp", sub)) :: noteObserver

    member _.CreateActor user aggId traceId cv =
        CommandService.createActor actor user aggId traceId cv

    member _.CreateNote user aggId traceId cv =
        CommandService.createNote note user aggId traceId cv

    member _.ChangeNote user aggId traceId cv =
        CommandService.changeNote note user aggId traceId cv

    member _.BatchChangeNote user aggId traceId cv =
        CommandService.batchChangeNote note user aggId traceId cv

    member _.GetNote aggId =
        Mutable.get note aggId

    member _.GetNoteObserver key = async {
        match noteObserver with
        | [] -> return failwith "观察者不存在。"
        | n -> return! (n |> List.map (fun n -> Observer.get n key) |> Async.Parallel)
    }