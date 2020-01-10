namespace Note.Server

open Note.Contract
open Note.Application


type NoteService (app: AppService) =
    inherit Note.NoteBase()

    override _.CreateNoteCommand (request, context) =
        app.CreateNote request

    override _.ChangeNoteCommand (request, context) =
        app.ChangeNote request