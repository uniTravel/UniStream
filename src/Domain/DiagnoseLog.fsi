namespace UniStream.Domain

open System


/// <summary>日志级别
/// </summary>
type internal LogLevel =
    | Trace = 0
    | Debug = 1
    | Info = 2
    | Warn = 3
    | Error = 4
    | Critical = 5


/// <summary>诊断日志模块
/// </summary>
[<RequireQualifiedAccess>]
module DiagnoseLog =

    /// <summary>诊断日志记录器
    /// </summary>
    type Logger

    /// <summary>创建诊断日志记录器
    /// </summary>
    /// <param name="name">日志名称。</param>
    /// <param name="logFunc">诊断日志流存储函数。</param>
    val logger :
        name: string ->
        logFunc: (string -> ReadOnlyMemory<byte> -> Async<unit>) ->
        Logger

    type Logger with

        /// <summary>记录Trace级别的诊断日志
        /// </summary>
        /// <param name="format">字符串格式。</param>
        member Trace :
            format: Printf.StringFormat<'a, unit> ->
            'a

        /// <summary>记录Debug级别的诊断日志
        /// </summary>
        /// <param name="format">字符串格式。</param>
        member Debug :
            format: Printf.StringFormat<'a, unit> ->
            'a

        /// <summary>记录Info级别的诊断日志
        /// </summary>
        /// <param name="format">字符串格式。</param>
        member Info :
            format: Printf.StringFormat<'a, unit> ->
            'a

        /// <summary>记录Warn级别的诊断日志
        /// </summary>
        /// <param name="format">字符串格式。</param>
        member Warn :
            format: Printf.StringFormat<'a, unit> ->
            'a

        /// <summary>记录Error级别的诊断日志
        /// </summary>
        /// <param name="stack">错误堆栈。</param>
        /// <param name="format">字符串格式。</param>
        member Error :
            stack: string ->
            format: Printf.StringFormat<'a, unit> ->
            'a

        /// <summary>记录Critical级别的诊断日志
        /// </summary>
        /// <param name="stack">错误堆栈。</param>
        /// <param name="format">字符串格式。</param>
        member Critical :
            stack: string ->
            format: Printf.StringFormat<'a, unit> ->
            'a