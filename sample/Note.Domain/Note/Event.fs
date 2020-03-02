namespace Note.Domain


type NoteCreated = { Title: string; Content: string }

type NoteChanged = { Content: string }