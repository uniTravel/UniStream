module Note.Tests

open System
open System.Text.Json
open Expecto
open UniStream.Domain
open Note.Contract.AssemblyInfo
open Note.Domain.NoteAgg

let inline getLongId (node: ^t) = (^t:(member LongId: string) node)

let inline (+@) x y = x + x * y

let inline get (agg: ^t) = (^t:(member Title: string) agg)

[<Tests>]
let tests =
    testList "Note" [
        testCase "序列化" <| fun _ ->
            let cn = { Title = "title1"; Content = "" }
            let x = JsonSerializer.SerializeToUtf8Bytes cn
            // let span = ReadOnlySpan x
            // let de = JsonSerializer.Deserialize<CreateNote> span
            printfn "Done!"
    ]