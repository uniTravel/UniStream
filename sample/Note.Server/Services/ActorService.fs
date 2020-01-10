namespace Note.Server

open Note.Contract
open Note.Application


type ActorService (app: AppService) =
    inherit Actor.ActorBase()

    override _.CreateActorCommand (request, context) =
        app.CreateActor request