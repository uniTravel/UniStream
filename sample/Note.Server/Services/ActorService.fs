namespace Note.Server

open Note.Contract
open Note.Application


type ActorService (app: AppService) =
    inherit Actor.ActorBase()

    override _.CreateActor (request, context) =
        app.CreateActor request