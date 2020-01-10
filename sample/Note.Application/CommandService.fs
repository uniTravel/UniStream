namespace Note.Application

open System
open UniStream.Domain
open Note.Contract
open Note.Domain

module CommandService =

    let createActor actor delta = async {
        let aggId = Guid.NewGuid()
        let traceId = Guid.NewGuid()
        let command = CreateActor.create delta
        do! Aggregator.applyCommand actor aggId traceId command
        let reply = CreateActorReply()
        reply.AggId <- aggId.ToString()
        reply.TraceId <- traceId.ToString()
        return reply
    }

    let createNote note delta = async {
        let aggId = Guid.NewGuid()
        let traceId = Guid.NewGuid()
        let command = CreateNote.create delta
        do! Aggregator.applyCommand note aggId traceId command
        let reply = CreateNoteReply()
        reply.AggId <- aggId.ToString()
        reply.TraceId <- traceId.ToString()
        return reply
    }

    let changeNote note (delta: ChangeNote) = async {
        let aggId = Guid delta.AggId
        let traceId = Guid.NewGuid()
        let command = ChangeNote.create delta
        do! Aggregator.applyCommand note aggId traceId command
        let reply = ChangeNoteReply()
        reply.TraceId <- traceId.ToString()
        return reply
    }