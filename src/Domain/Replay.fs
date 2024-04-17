namespace UniStream.Domain

open System
open System.Text.Json


/// <summary>重播类型
/// <para>重播二进制存储的事件以再建聚合。</para>
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
/// <typeparam name="'evt">事件类型。</typeparam>
[<Sealed>]
type Replay<'agg, 'evt when Evt<'agg, 'evt>>() =

    /// <summary>事件类型全称
    /// </summary>
    member inline _.FullName = typeof<'evt>.FullName

    /// <summary>重播函数
    /// </summary>
    member inline _.Act =
        fun (agg: 'agg) (evtData: ReadOnlyMemory<byte>) ->
            let evt = JsonSerializer.Deserialize<'evt> evtData.Span
            evt.Apply agg
