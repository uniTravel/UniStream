namespace Domain


type NoteUpgraded =
    { Up: int }

    member me.Apply(agg: Note) = agg.Grade <- me.Up

type UpgradeNote =
    { Up: int }

    member me.Validate(agg: Note) =
        if agg.Grade + me.Up > 3 then
            raise <| ValidateError "等级不得大于3。"

    member me.Execute(agg: Note) : NoteUpgraded = { Up = agg.Grade + me.Up }
