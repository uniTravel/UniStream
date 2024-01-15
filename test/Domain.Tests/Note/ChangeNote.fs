namespace Domain


type NoteChanged =
    { Content: string }

    member me.Apply(agg: Note) = agg.Content <- me.Content


type ChangeNote =
    { Content: string }

    member me.Validate(agg: Note) = ()

    member me.Execute(agg: Note) : NoteChanged = { Content = me.Content }
