namespace Note.Application

open System
open UniStream.Domain
open Note.Contract
open Note.Domain

module CommandService =

    let createActor actor cv = async {
        let aggId = Guid.NewGuid()
        let traceId = Guid.NewGuid()
        let command = CreateActor.create cv
        do! Aggregator.executeCommand actor aggId traceId command
        let reply = CreateActorReply()
        reply.AggId <- aggId.ToString()
        reply.TraceId <- traceId.ToString()
        return reply
    }

    let createNote note cv = async {
        let aggId = Guid.NewGuid()
        let traceId = Guid.NewGuid()
        let command = CreateNote.create cv
        do! Aggregator.executeCommand note aggId traceId command
        let reply = CreateNoteReply()
        reply.AggId <- aggId.ToString()
        reply.TraceId <- traceId.ToString()
        return reply
    }

    let changeNote note (cv: ChangeNote) = async {
        let aggId = Guid cv.AggId
        let traceId = Guid.NewGuid()
        let command = ChangeNote.create cv
        do! Aggregator.executeCommand note aggId traceId command
        let reply = ChangeNoteReply()
        reply.TraceId <- traceId.ToString()
        return reply
    }