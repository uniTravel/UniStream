namespace Domain


type UpgradeNote = { Up: int }

type UpgradeNote with

    member me.Validate(agg: Note) =
        if agg.Grade + me.Up > 3 then
            raise <| ValidateError "等级不得大于3。"

    member me.Execute(agg: Note) = agg.Grade <- agg.Grade + me.Up
