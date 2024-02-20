namespace EventStore.Tests


type NoteCreated =
    { Title: string
      Content: string
      Grade: int }

type NoteChanged = { Content: string }

type NoteUpgraded = { Up: int }
