namespace UniStream.Domain

open System
open System.Text.Json


/// <summary>重播类型
/// <para>重播二进制存储的命令以再建聚合。</para>
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
/// <typeparam name="'com">命令类型。</typeparam>
[<Sealed>]
type Replay<'agg, 'com when Com<'agg, 'com>>() =

    /// <summary>命令类型全称
    /// </summary>
    member inline _.FullName = typeof<'com>.FullName

    /// <summary>执行重播的函数
    /// </summary>
    member inline _.Act =
        fun (agg: 'agg) (comData: ReadOnlyMemory<byte>) ->
            let com = JsonSerializer.Deserialize<'com> comData.Span
            com.Execute agg
