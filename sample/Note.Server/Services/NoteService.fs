namespace Note.Server

open System
open Note.Contract
open Note.Application


type NoteService (app: AppService) =
    inherit Note.NoteBase()

    override _.CreateNote (request, context) =
        Async.StartAsTask <| async {
            let aggId = Guid.NewGuid()
            let traceId = Guid.NewGuid()
            let! note = app.CreateNote "test" aggId traceId { Title = request.Title; Content = request.Content }
            let reply = CreateNoteReply()
            reply.AggId <- aggId.ToString()
            reply.TraceId <- traceId.ToString()
            return reply
        }

    override _.ChangeNote (request, context) =
        Async.StartAsTask <| async {
            let aggId = Guid request.AggId
            let traceId = Guid.NewGuid()
            let! note = app.ChangeNote "test" aggId traceId { Content = request.Content}
            let reply = ChangeNoteReply()
            reply.TraceId <- traceId.ToString()
            return reply
        }