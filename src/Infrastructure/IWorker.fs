namespace UniStream.Domain

open System.Threading


type IWorker =

    abstract member Launch: ct: CancellationToken -> Tasks.Task
