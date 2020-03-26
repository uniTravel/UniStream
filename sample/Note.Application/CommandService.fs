namespace Note.Application

open UniStream.Domain
open Note.Domain


module CommandService =

    let createActor actor user aggId traceId cv = async {
        let command = CreateActor.create cv
        return! Aggregator.immutableCommand actor user aggId traceId command
    }

    let createNote note user aggId traceId cv = async {
        let command = CreateNote.create cv
        return! Aggregator.mutableCommand note user aggId traceId command
    }

    let changeNote note user aggId traceId cv = async {
        let command = ChangeNote.create cv
        return! Aggregator.mutableCommand note user aggId traceId command
    }