namespace Domain


type CreateNote =
    { Title: string
      Content: string
      Grade: int }

type CreateNote with

    member me.Validate(agg: Note) =
        if me.Grade > 3 then
            raise <| ValidateError "等级不得大于3。"

    member me.Execute(agg: Note) =
        agg.Title <- me.Title
        agg.Content <- me.Content
        agg.Grade <- me.Grade
