namespace Note.Application

open UniStream.Domain
open Note.Domain


module CommandService =

    let createActor actor user aggId traceId cv = async {
        let command = CreateActor.create cv
        return! Immutable.apply actor user aggId traceId command }

    let createNote note user aggId traceId cv = async {
        let command = CreateNote.create cv
        return! Mutable.apply note user aggId traceId command }

    let changeNote note user aggId traceId cv = async {
        let command = ChangeNote.create cv
        return! Mutable.apply note user aggId traceId command }

    let appendNote (note: Observer.T<NoteObserver.T>) aggId number evType data = async {
        return! Observer.append note aggId number evType data }