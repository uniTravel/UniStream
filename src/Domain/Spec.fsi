namespace UniStream.Domain


/// <summary>规格模块
/// </summary>
[<RequireQualifiedAccessAttribute>]
module Spec =

    /// <summary>规格与
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="left">左侧规格。</param>
    /// <param name="right">右侧规格。</param>
    /// <param name="obj">规格适用对象。</param>
    /// <returns>规格验证结果</returns>
    val inline a:
        [<InlineIfLambda>] left: ('agg -> bool) -> [<InlineIfLambda>] right: ('agg -> bool) -> obj: 'agg -> bool
            when 'agg :> Aggregate

    /// <summary>规格或
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="left">左侧规格。</param>
    /// <param name="right">右侧规格。</param>
    /// <param name="obj">规格适用对象。</param>
    /// <returns>规格验证结果</returns>
    val inline o:
        [<InlineIfLambda>] left: ('agg -> bool) -> [<InlineIfLambda>] right: ('agg -> bool) -> obj: 'agg -> bool
            when 'agg :> Aggregate

    /// <summary>规格异
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="spec">规格。</param>
    /// <param name="obj">规格适用对象。</param>
    /// <returns>规格验证结果</returns>
    val inline n: [<InlineIfLambda>] spec: ('agg -> bool) -> obj: 'agg -> bool when 'agg :> Aggregate
