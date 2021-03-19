namespace Note.Application

open UniStream.Domain
open Note


module CommandService =

    let createActor actor aggKey traceId cmd = async {
        let command = CreateActor.create cmd
        return! Immutable.apply actor aggKey traceId command }

    let createNote note aggKey traceId cmd = async {
        let command = CreateNote.create cmd
        return! Mutable.apply note aggKey traceId command }

    let changeNote note aggKey traceId cmd = async {
        let command = ChangeNote.create cmd
        return! Mutable.apply note aggKey traceId command }

    let appendNote (obs: Observer.T<NoteObserver.T>) aggKey number evType data = async {
        return! Observer.append obs aggKey number evType data }