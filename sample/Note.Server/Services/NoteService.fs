namespace Note.Server

open Note.Contract
open Note.Application


type NoteService (app: AppService) =
    inherit Note.NoteBase()

    override _.CreateNote (request, context) =
        app.CreateNote request

    override _.ChangeNote (request, context) =
        app.ChangeNote request