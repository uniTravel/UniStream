module Domain.Tests.Metadata

open System
open System.Text
open Expecto
open UniStream.Domain


type NoteCreated = { Title: string; Content: string }

module M =
    let f x = nameof x

[<Tests>]
let tests =
    testSequenced <| testList "Metadata" [
        testCase "1" <| fun _ ->
            let note = { Title = "title"; Content = "content" }
            let q = nameof M.f
            printfn $"{M.f 12}"
            printfn $"{nameof M}"
            printfn $"{nameof M.f}"

    ] |> testLabel "Domain"