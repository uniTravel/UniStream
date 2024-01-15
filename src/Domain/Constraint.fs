namespace UniStream.Domain

open System


/// <summary>聚合约束组
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
type Agg<'agg when 'agg :> Aggregate> = 'agg

/// <summary>事件约束组
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
/// <typeparam name="'evt">事件类型。</typeparam>
type Evt<'agg, 'evt when 'agg :> Aggregate and 'evt: (member Apply: 'agg -> unit)> = 'evt

/// <summary>命令约束组
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
/// <typeparam name="'com">命令类型。</typeparam>
/// <typeparam name="'evt">事件类型。</typeparam>
type Com<'agg, 'com, 'evt
    when 'agg :> Aggregate
    and 'com: (member Validate: 'agg -> unit)
    and 'com: (member Execute: 'agg -> 'evt)
    and 'evt: (member Apply: 'agg -> unit)> = 'com

/// <summary>重播约束组
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
/// <typeparam name="'rep">重播类型。</typeparam>
type Rep<'agg, 'rep
    when 'agg :> Aggregate
    and 'rep: (member FullName: string)
    and 'rep: (member Act: ('agg -> ReadOnlyMemory<byte> -> unit))> = 'rep
