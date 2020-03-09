namespace Note.Application

open UniStream.Domain
open Note.Domain


module CommandService =

    let createActor actor aggId traceId cv = async {
        let command = CreateActor.create cv
        return! Aggregator.executeCommand actor aggId traceId command
    }

    let createNote note aggId traceId cv = async {
        let command = CreateNote.create cv
        return! Aggregator.executeCommand note aggId traceId command
    }

    let changeNote note aggId traceId cv = async {
        let command = ChangeNote.create cv
        return! Aggregator.executeCommand note aggId traceId command
    }