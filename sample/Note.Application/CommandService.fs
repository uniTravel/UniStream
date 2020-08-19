namespace Note.Application

open UniStream.Domain
open Note.Domain


module CommandService =

    let createActor actor user aggId traceId cmd = async {
        let command = CreateActor.create cmd
        return! Immutable.apply actor user aggId traceId command }

    let createNote note user aggId traceId cmd = async {
        let command = CreateNote.create cmd
        return! Mutable.apply note user aggId traceId command }

    let changeNote note user aggId traceId cmd = async {
        let command = ChangeNote.create cmd
        return! Mutable.apply note user aggId traceId command }

    let appendNote (note: Observer.T<NoteObserver.T>) aggId number evType data = async {
        return! Observer.append note aggId number evType data }