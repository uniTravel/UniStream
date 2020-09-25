namespace Note.Application

open UniStream.Domain
open Note


module CommandService =

    let createActor actor aggId traceId cmd = async {
        let command = CreateActor.create cmd
        return! Immutable.apply actor aggId traceId command }

    let createNote note aggId traceId cmd = async {
        let command = CreateNote.create cmd
        return! Mutable.apply note aggId traceId command }

    let changeNote note aggId traceId cmd = async {
        let command = ChangeNote.create cmd
        return! Mutable.apply note aggId traceId command }

    let appendNote (note: Observer.T<NoteObserver.T>) aggId number evType data = async {
        return! Observer.append note aggId number evType data }