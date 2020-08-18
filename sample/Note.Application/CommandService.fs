namespace Note.Application

open UniStream.Domain
open Note.Domain


module CommandService =

    let createActor (actor: Immutable.T<Actor.T>) user aggId cvType traceId data = async {
        return! Immutable.apply actor user aggId cvType traceId data }

    let createNote (note: Mutable.T<Note.T>) user aggId cvType traceId data = async {
        return! Mutable.apply note user aggId cvType traceId data }

    let changeNote (note: Mutable.T<Note.T>) user aggId cvType traceId data = async {
        return! Mutable.apply note user aggId cvType traceId data }

    let appendNote (note: Observer.T<NoteObserver.T>) aggId number evType data = async {
        return! Observer.append note aggId number evType data }