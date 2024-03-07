module EventStore.Tests.Stream

open System
open System.Text.Json
open Expecto
open EventStore.Client
open UniStream.Infrastructure


let conn =
    "esdb://admin:changeit@127.0.0.1:2111,127.0.0.1:2112,127.0.0.1:2113?tls=true&tlsVerifyCert=false"

let client = new EventStoreClient(EventStoreClientSettings.Create(conn))
let es = Stream(client)
let traceId = Some <| Guid.NewGuid()
let aggId = Guid.NewGuid()


[<Tests>]
let test =
    [ testCase "写入第一个事件"
      <| fun _ ->
          let note =
              { Title = "t"
                Content = "c"
                Grade = 1 }

          let data = JsonSerializer.SerializeToUtf8Bytes note
          es.Write None "Note" aggId UInt64.MaxValue "NoteCreated" data
          let result = es.Read "Note" aggId
          Expect.hasLength result 1 "写入结果有误"
          Expect.equal result[0] ("NoteCreated", data) "写入结果有误"
      testCase "写入第二个事件"
      <| fun _ ->
          let note = { Content = "c1" }
          let data = JsonSerializer.SerializeToUtf8Bytes note
          es.Write traceId "Note" aggId 0UL "NoteChanged" data
          let result = es.Read "Note" aggId
          Expect.hasLength result 2 "写入结果有误"
          Expect.equal result[1] ("NoteChanged", data) "写入结果有误"
      testCase "写入第三个事件，但Revision有误"
      <| fun _ ->
          let note = { Up = 2 }
          let data = JsonSerializer.SerializeToUtf8Bytes note
          let f = fun _ -> es.Write traceId "Note" aggId 0UL "NoteUpgraded" data
          Expect.throws f "异常有误"
      testCase "写入第三个事件"
      <| fun _ ->
          let note = { Up = 2 }
          let data = JsonSerializer.SerializeToUtf8Bytes note
          es.Write traceId "Note" aggId 1UL "NoteUpgraded" data
          let result = es.Read "Note" aggId
          Expect.hasLength result 3 "写入结果有误"
          Expect.equal result[2] ("NoteUpgraded", data) "写入结果有误" ]
    |> testList "Stream"
    |> testSequenced
    |> testLabel "EventStore"
