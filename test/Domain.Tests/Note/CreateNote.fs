namespace Domain

open UniStream.Domain


type NoteCreated =
    { Title: string
      Content: string
      Grade: int }

    member me.Apply(agg: Note) =
        agg.Title <- me.Title
        agg.Content <- me.Content
        agg.Grade <- me.Grade


type CreateNote =
    { Title: string
      Content: string
      Grade: int }

    member me.Validate(agg: Note) =
        if me.Grade > 3 then
            raise <| ValidateException "等级不得大于3。"

    member me.Execute(agg: Note) : NoteCreated =
        { Title = me.Title
          Content = me.Content
          Grade = me.Grade }
