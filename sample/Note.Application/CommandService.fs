namespace Note.Application

open System
open UniStream.Domain
open Note.Contract
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

    let changeNote note traceId (cv: ChangeNote) = async {
        let aggId = Guid cv.AggId
        let command = ChangeNote.create cv
        return! Aggregator.executeCommand note aggId traceId command
    }