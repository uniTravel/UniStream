namespace UniStream.Domain

open System
open System.Text.Json


/// <summary>重播类型
/// <para>重播二进制存储的变更以再建聚合。</para>
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
/// <typeparam name="'chg">变更类型。</typeparam>
type Replay<'agg, 'chg when Chg<'agg, 'chg>>() =

    /// <summary>变更类型全称
    /// </summary>
    member inline _.FullName = typeof<'chg>.FullName

    /// <summary>重播函数
    /// </summary>
    member inline _.Act =
        fun (agg: 'agg) (chgData: ReadOnlyMemory<byte>) ->
            let chg = JsonSerializer.Deserialize<'chg> chgData.Span
            chg.Execute agg
