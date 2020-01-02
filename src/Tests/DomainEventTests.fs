module DomainEvent.Tests

open System
open System.Text
open Expecto
open UniStream.Infrastructure

let uri = Uri "tcp://admin:changeit@localhost:4011"
let conn = DomainEvent.create uri
let esFunc = DomainEvent.write conn
let aggId = Guid.NewGuid()

[<Tests>]
let tests =
    testSequenced <| testList "DomainEvent" [
        testCase "1" <| fun _ ->
            let traceId = Guid.NewGuid()
            let aggType = "Note"
            let deltaType = "NoteCreated"
            let delta = Encoding.UTF8.GetBytes "Soma data"
            esFunc aggType aggId 0L traceId deltaType delta
        testCase "2" <| fun _ ->
            let traceId = Guid.NewGuid()
            let aggType = "Note"
            let deltaType = "NoteChanged"
            let delta = Encoding.UTF8.GetBytes "Soma data1"
            esFunc aggType aggId 1L traceId deltaType delta
        testCase "3" <| fun _ ->
            let traceId = Guid.NewGuid()
            let aggType = "Note"
            let deltaType = "NoteChanged"
            let delta = Encoding.UTF8.GetBytes "Soma data2"
            esFunc aggType aggId 2L traceId deltaType delta
    ]
    |> testLabel "UniStream.Domain"