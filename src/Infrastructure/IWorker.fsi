namespace UniStream.Domain

open System.Threading


/// <summary>后台任务接口
/// </summary>
[<Interface>]
type IWorker =

    /// <summary>启动后台任务
    /// </summary>
    /// <param name="ct">取消凭据。</param>
    abstract member Launch: ct: CancellationToken -> Tasks.Task
