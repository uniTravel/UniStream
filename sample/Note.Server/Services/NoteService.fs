namespace Note.Server

open System
open Note.Contract
open Note.Application


type NoteService (app: AppService) =
    inherit Note.NoteBase()

    override _.CreateNoteCommand (request, context) =
        Async.StartAsTask <| async {
            let aggId = Guid.NewGuid()
            let traceId = Guid.NewGuid()
            let! note = app.CreateNote aggId traceId request
            let reply = CreateNoteReply()
            reply.AggId <- aggId.ToString()
            reply.TraceId <- traceId.ToString()
            return reply
        }

    override _.ChangeNoteCommand (request, context) =
        Async.StartAsTask <| async {
            let traceId = Guid.NewGuid()
            let! note = app.ChangeNote traceId request
            let reply = ChangeNoteReply()
            reply.TraceId <- traceId.ToString()
            return reply
        }