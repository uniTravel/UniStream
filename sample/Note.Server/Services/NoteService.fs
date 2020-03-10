namespace Note.Server

open System
open Note.Contract
open Note.Application


type NoteService (app: AppService) =
    inherit Note.NoteBase()

    override _.CreateNote (request, context) =
        Async.StartAsTask <| async {
            let aggId = Guid.NewGuid().ToString()
            let traceId = Guid.NewGuid().ToString()
            let! note = app.CreateNote aggId traceId { Title = request.Title; Content = request.Content }
            let reply = CreateNoteReply()
            reply.AggId <- aggId.ToString()
            reply.TraceId <- traceId.ToString()
            return reply
        }

    override _.ChangeNote (request, context) =
        Async.StartAsTask <| async {
            let aggId = request.AggId
            let traceId = Guid.NewGuid().ToString()
            let! note = app.ChangeNote aggId traceId { Content = request.Content}
            let reply = ChangeNoteReply()
            reply.TraceId <- traceId.ToString()
            return reply
        }