module Deserialize.Tests

open System
open System.IO
open System.Text.Json
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open FSharp.Quotations.Evaluator.QuotationEvaluationExtensions
open Expecto
open UniStream.Domain
open FSharp.Quotations.Evaluator

[<CLIMutable>]
type CreateNote = { Title: string; Content: string } with
    interface IValue
    static member Des = DomainEvent.fromBytes<CreateNote>
[<CLIMutable>]
type ChangeNote = { Content: string } with
    interface IValue
    static member Des = DomainEvent.fromBytes<ChangeNote>

// let inline name< ^T when ^T: (static member Des: (byte[] -> ^T)) > =
//     (^T: (static member Des: (byte[] -> ^T)) ())
let inline name (_: ^T when ^T: (static member Des: (byte[] -> ^T))) =
    (^T: (static member Des: (byte[] -> ^T)) ())


let rec eval = function
    | Value (v, t) -> v
    | Call (None, mi, args) -> mi.Invoke(null, evalAll args)
    | arg -> raise <| System.NotSupportedException (arg.ToString ())
and evalAll args = [| for arg in args -> eval arg |]



[<FTests>]
let tests =
    testList "反序列化" [
        testCase "1" <| fun _ ->
            let cn = { Title = "title1"; Content = "" }
            let x = JsonSerializer.SerializeToUtf8Bytes cn
            // let de = DomainEvent.fromBytes<CreateNote> x
            // let f = des.Compile ()
            // let de = f x
            // let q = FSharpType.GetRecordFields typeof<CreateNote>
            // let s = Expr.NewRecord (typeof<int ref>, [ <@@ 4 @@> ])
            // let r = <@@ { contents = 5 } @@>
            let q = eval <@ 1 + 3 @>


            printfn "Done!"
    ]
    |> testLabel "UniStream.Domain"