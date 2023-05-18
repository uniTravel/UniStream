module Domain.Tests.Serialize

open System
open Expecto
open UniStream.Domain
open Domain.Models

[<Tests>]
let test =
    testList
        "Serialize"
        [ testCase "序列化领域命令"
          <| fun _ ->
              let createNote: CreateNote =
                  { Title = "title"
                    Content = "content1" }

              let changeNote: ChangeNote = { Content = "content2" }
              let s1 = Delta.serialize createNote
              let s2 = Delta.serialize changeNote
              let d1 = Delta.deserialize<CreateNote> s1
              let d2 = Delta.deserialize<ChangeNote> s2
              Expect.equal d1 createNote ""
              Expect.equal d2 changeNote ""
          testCase "序列化领域事件"
          <| fun _ ->
              let noteCreated: NoteCreated =
                  { Title = "title"
                    Content = "content1" }

              let noteChanged: NoteChanged = { Content = "content2" }
              let s1 = Delta.serialize noteCreated
              let s2 = Delta.serialize noteChanged
              let d1 = Delta.deserialize<NoteCreated> s1
              let d2 = Delta.deserialize<NoteChanged> s2
              Expect.equal d1 noteCreated ""
              Expect.equal d2 noteChanged ""
          testCase "序列化聚合"
          <| fun _ ->
              let note = Note(Guid.NewGuid())
              let s = Delta.serialize note
              let d = Delta.deserialize<Note> s
              Expect.equal d.Id note.Id ""
              Expect.equal d.Revision note.Revision "" ]
    |> testLabel "Domain"
