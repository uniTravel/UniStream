module Domain.Tests
#nowarn "9"

open System
open System.Text
open System.Text.Json
open Expecto
open UniStream.Domain
open System.IO
open Microsoft.FSharp.NativeInterop


type BlockBuilder () =

    static member inline SetValue (dst, value) =
        let dst = NativePtr.ofNativeInt dst
        NativePtr.write dst value
        NativePtr.add dst 1 |> NativePtr.toNativeInt

    [<CustomOperation("push")>]
    member inline _.Push ((len, trans), x: 't) =
        let trans (dst: nativeint) =
            let dst = trans dst
            BlockBuilder.SetValue (dst, x)
        (len + sizeof<'t>, trans)

    // [<CustomOperation("pushString")>]
    // member inline _.PushString ((len, trans), x: string) =
    //     let trans (dst: nativeint) =
    //         let dst = trans dst
    //         BlockBuilder.SetValue (dst, x)
    //     (len + sizeof<'t>, trans)


    member inline _.Run ((index, trans: nativeint -> nativeint)) =
        let arr = Array.zeroCreate<byte> index
        use dst = fixed arr
        NativePtr.toNativeInt dst |> trans |> ignore
        arr

    member inline _.Yield _ = 0, id

let block = BlockBuilder ()


type ParseBuilder() =

    member inline _.Yield _ = (fun f _ -> f), 0

    [<CustomOperation "popByte">]
    member inline _.PopByte ((statf, i)) =
        let reduce handle (x: byte[]) = (statf handle x) (x.[i])
        (reduce, i + 1)

    [<CustomOperation "popInt32">]
    member inline _.PopInt32 ((statf, i)) =
        let conv x = BitConverter.ToInt32 (x, i)
        let trans handle x = (statf handle x) (conv x)
        (trans, i + 4)

    [<CustomOperation "popGuid">]
    member inline _.PopGuid ((statf, i)) =
        let conv x =
            let bytes = Array.zeroCreate 16
            Buffer.BlockCopy (x, i, bytes, 0, 16)
            Guid bytes
        let trans handle x = (statf handle x) (conv x)
        (trans, i + 16)

    member _.Run f = match f  with | a, _ -> a


let parse = ParseBuilder ()

let foo handle arr =
    let f = parse {
        // popByte
        // popGuid
        popInt32
    }
    f handle arr

[<CLIMutable>]
type MetaEvent = { AggregateId: Guid; TraceId: Guid; Version: int }

[<Tests>]
let tests =
    testList "Domain" [
        testCase "MetaLog" <| fun _ ->
            // let metaLog = DomainLog.createMeta <| Guid.NewGuid ()
            // let ms = DomainLog.asMetaBytes metaLog
            // let ml = DomainLog.fromMetaBytes ms
            // Expect.equal ml metaLog "值不等"
            // let e = { AggregateId = Guid.NewGuid (); TraceId = Guid.NewGuid (); Version = 7 }
            // let array = JsonSerializer.SerializeToUtf8Bytes e
            // let span = ReadOnlySpan array
            // let de = JsonSerializer.Deserialize<MetaEvent> span
            // let q = de.Version

            // let x = BitConverter.GetBytes 1
            // let y = BitConverter.GetBytes 2
            // let arr = Array.concat [ x; y ]
            // let add m = m + 1
            // let q = foo add arr

            let s = "test"

            let x = block {
                push 1
                push 2
            }

            printfn "Done!"
    ]