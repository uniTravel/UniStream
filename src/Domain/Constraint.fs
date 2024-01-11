namespace UniStream.Domain

open System


/// <summary>聚合约束组
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
type Agg<'agg when 'agg :> Aggregate> = 'agg

/// <summary>变更约束组
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
/// <typeparam name="'chg">变更类型。</typeparam>
type Chg<'agg, 'chg
    when 'agg :> Aggregate and 'chg: (member Validate: 'agg -> unit) and 'chg: (member Execute: 'agg -> unit)> = 'chg

/// <summary>重播约束组
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
/// <typeparam name="'rep">重播类型。</typeparam>
type Rep<'agg, 'rep
    when 'agg :> Aggregate
    and 'rep: (member FullName: string)
    and 'rep: (member Act: ('agg -> ReadOnlyMemory<byte> -> unit))> = 'rep
