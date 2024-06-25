namespace UniStream.Domain

open System.Threading


type IWorker<'agg when 'agg :> Aggregate> =

    abstract member Launch: ct: CancellationToken -> Tasks.Task
