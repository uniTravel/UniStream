namespace UniStream.Domain


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

    /// <summary>诊断日志
    /// </summary>
    type T

    /// <summary>诊断日志记录器
    /// </summary>
    type Logger

    /// <summary>创建诊断日志记录器
    /// </summary>
    /// <param name="name">日志名称。</param>
    /// <param name="logFunc">诊断日志流存储函数。</param>
    val logger : string -> (string -> byte[] -> unit) -> Logger

    /// <summary>转成字节数组
    /// <para>诊断日志数据采用UTF8格式的Json序列化。</para>
    /// </summary>
    /// <param name="log">诊断日志数据。</param>
    /// <returns>诊断日志数据的字节数组。</returns>
    val private asBytes : T -> byte[]

    /// <summary>转成诊断日志数据
    /// <para>诊断日志数据采用UTF8格式的Json反序列化。</para>
    /// </summary>
    /// <param name="bytes">诊断日志数据的字节数组。</param>
    /// <returns>诊断日志数据。</returns>
    val private fromBytes : byte[] -> T

    type Logger with

        /// <summary>记录Trace级别的诊断日志
        /// </summary>
        /// <param name="format">字符串格式。</param>
        member Trace : Printf.StringFormat<'a, unit> -> 'a

        /// <summary>记录Debug级别的诊断日志
        /// </summary>
        /// <param name="format">字符串格式。</param>
        member Debug : Printf.StringFormat<'a, unit> -> 'a

        /// <summary>记录Info级别的诊断日志
        /// </summary>
        /// <param name="format">字符串格式。</param>
        member Info : Printf.StringFormat<'a, unit> -> 'a

        /// <summary>记录Warn级别的诊断日志
        /// </summary>
        /// <param name="format">字符串格式。</param>
        member Warn : Printf.StringFormat<'a, unit> -> 'a

        /// <summary>记录Error级别的诊断日志
        /// </summary>
        /// <param name="stack">错误堆栈。</param>
        /// <param name="format">字符串格式。</param>
        member Error : string -> Printf.StringFormat<'a, unit> -> 'a

        /// <summary>记录Critical级别的诊断日志
        /// </summary>
        /// <param name="stack">错误堆栈。</param>
        /// <param name="format">字符串格式。</param>
        member Critical : string -> Printf.StringFormat<'a, unit> -> 'a