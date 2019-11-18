namespace UniStream.Domain


// [<RequireQualifiedAccess>]
// module Aggregate =

    // type IWrappedAggregate<'agg when 'agg :> IAggregate> =
    //     abstract Value : 'agg

    // val create : bool -> ('a -> 'agg) -> 'agg

    // val apply : System.Guid -> IDomainCommand<'agg> -> Async<unit>


// [<Sealed>]
// type Aggregate<'agg when 'agg :> IAggregate> =

//     /// <summary>构造函数
//     /// </summary>
//     /// <param name="eventStore">事件流存储函数。</param>
//     /// <param name="logStore">日志流存储函数。</param>
//     /// <param name="blockSeconds">挂起超过设定的秒数，阻塞聚合请求。</param>
//     new :
//         IStore *
//         IStore *
//         int64 -> Aggregate<'agg>

//     /// <summary>执行命令
//     /// </summary>
//     /// <param name="id">聚合ID。</param>
//     /// <param name="command">待执行的命令。</param>
//     /// <returns>命令执行结果。</returns>
//     member Apply : System.Guid -> IDomainCommand<'agg> -> Async<unit>