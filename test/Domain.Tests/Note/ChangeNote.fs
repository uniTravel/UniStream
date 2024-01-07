namespace Domain


type ChangeNote = { Content: string }

type ChangeNote with

    member me.Validate(agg: Note) = ()

    member me.Execute(agg: Note) = agg.Content <- me.Content
