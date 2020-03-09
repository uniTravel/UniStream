namespace Note.Server

open System
open Note.Contract
open Note.Application


type ActorService (app: AppService) =
    inherit Actor.ActorBase()

    override _.CreateActor (request, context) =
        Async.StartAsTask <| async {
            let aggId = Guid.NewGuid()
            let traceId = Guid.NewGuid()
            let! actor = app.CreateActor aggId traceId { Name = request.Name }
            let reply = CreateActorReply()
            reply.AggId <- aggId.ToString()
            reply.TraceId <- traceId.ToString()
            return reply
        }
