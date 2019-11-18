namespace UniStream.Domain

open System


type AccessRepository<'agg when 'agg :> IAggregate> =
    | Take of Guid * AsyncReplyChannel<Result<'agg, string>>
    | Put of 'agg * AsyncReplyChannel<Result<unit, string>>

module Aggregate =

    type IWrappedAggregate =
        abstract Value : IAggregate

    let create<'agg when 'agg :> IAggregate> isValid ctor (agg: 'agg) =
        if isValid agg
        then Some (ctor agg)
        else None

    let apply f (s: IWrappedAggregate) =
        s.Value |> f

    let value s = apply id s

    let agent get blockTicks =
        MailboxProcessor<AccessRepository<'agg>>.Start <| fun inbox ->
            let rec loop (repo: Repository<'agg>) = async {
                match! inbox.Receive () with
                | Take (id, channel) ->
                    try
                        let newRepo = repo.Take id get blockTicks channel
                        return! loop newRepo
                    with ex ->
                        channel.Reply <| Error ex.Message
                        return! loop repo
                | Put (agg, channel) ->
                    try
                        let newRepo = repo.Put agg blockTicks
                        channel.Reply <| Ok ()
                        return! loop newRepo
                    with ex ->
                        channel.Reply <| Error ex.Message
                        return! loop repo
            }
            loop Repository.empty





// [<Sealed>]
// type Aggregate<'agg when 'agg :> IAggregate> (get, append, retries, blockSeconds) =

//     let blockTicks = blockSeconds * 10000000L

//     let agent =
//         MailboxProcessor<AccessRepository<'agg>>.Start <| fun inbox ->
//             let rec loop (repo: Repository<System.Guid, 'agg>) = async {
//                 match! inbox.Receive () with
//                 | Take (id, channel) ->
//                     try
//                         let newRepo = repo.Take id get blockTicks channel
//                         return! loop newRepo
//                     with ex ->
//                         channel.Reply <| Error ex.Message
//                         return! loop repo
//                 | Put (agg, channel) ->
//                     try
//                         let newRepo = repo.Put agg blockTicks
//                         channel.Reply <| Ok ()
//                         return! loop newRepo
//                     with ex ->
//                         channel.Reply <| Error ex.Message
//                         return! loop repo
//             }
//             loop Repository.empty



//     let execute (agg: 'agg) (command: IDomainCommand<'agg>) =
//         try Ok <| command.Execute agg
//         with ex -> Error ex.Message

//     let trigger (agg: 'agg) (event: IDomainEvent<'agg>) =
//         try Ok <| (event.Trigger agg, event)
//         with ex -> Error ex.Message

//     let apply (agg': 'agg, event: IDomainEvent<'agg>) =
//         try
//             append event
//             Ok agg'
//         with ex -> Error ex.Message

//     let rec put (agg: 'agg) (retry: int) = async {
//         match! agent.PostAndAsyncReply (fun channel -> Put (agg, channel)) with
//         | Ok _ -> ()
//         | Error err ->
//             match retry with
//             | 0 -> failwithf "重试%d次，放回聚合出错：%s" retries err
//             | _ -> put agg (retry - 1) |> ignore
//     }

//     member _.Apply (id: System.Guid) (command: IDomainCommand<'agg>) = async {
//         match! agent.PostAndAsyncReply (fun channel -> Take (id, channel)) with
//         | Ok agg ->
//             let result =
//                 Ok command
//                 |> Result.bind (execute agg)
//                 |> Result.bind (trigger agg)
//                 |> Result.bind apply
//             match result with
//             | Ok agg' ->
//                 Async.Start <| put agg' retries
//             | Error err ->
//                 Async.Start <| put agg retries
//                 failwithf "执行命令出错：%s" err
//         | Error err -> failwithf "取出聚合出错：%s" err
//     }