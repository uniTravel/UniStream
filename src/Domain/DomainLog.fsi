namespace UniStream.Domain


/// <summary>领域日志模块
/// </summary>
[<RequireQualifiedAccess>]
module DomainLog =

    /// <summary>领域日志记录器
    /// </summary>
    type Logger

    /// <summary>创建领域日志记录器
    /// </summary>
    /// <param name="aggType">领域类型全名。</param>
    /// <param name="logFunc">领域日志流存储函数。</param>
    val logger : string -> (string -> string -> byte[] -> unit) -> Logger

    type Logger with

        /// <summary>记录Processing状态的领域日志
        /// </summary>
        /// <param name="cvType">领域命令值类型。</param>
        /// <param name="aggId">聚合ID。</param>
        /// <param name="traceId">跟踪ID。</param>
        /// <param name="format">字符串格式。</param>
        member Process : string -> string -> string -> Printf.StringFormat<'a, unit> -> 'a

        /// <summary>记录Successed状态的领域日志
        /// </summary>
        /// <param name="cvType">领域命令值类型。</param>
        /// <param name="aggId">聚合ID。</param>
        /// <param name="traceId">跟踪ID。</param>
        /// <param name="format">字符串格式。</param>
        member Success : string -> string -> string -> Printf.StringFormat<'a, unit> -> 'a

        /// <summary>记录Failed状态的领域日志
        /// </summary>
        /// <param name="cvType">领域命令值类型。</param>
        /// <param name="aggId">聚合ID。</param>
        /// <param name="traceId">跟踪ID。</param>
        /// <param name="format">字符串格式。</param>
        member Fail : string -> string -> string -> Printf.StringFormat<'a, unit> -> 'a