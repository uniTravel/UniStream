namespace UniStream.Domain


/// <summary>Kafka实现涉及的常量
/// </summary>
[<RequireQualifiedAccess>]
module Cons =

    /// <summary>用于聚合类型相关的命名配置、键控服务
    /// </summary>
    [<Literal>]
    let Typ = "Typ"

    /// <summary>用于聚合相关的命名配置、键控服务
    /// </summary>
    [<Literal>]
    let Agg = "Agg"

    /// <summary>用于命令相关的命名配置、键控服务
    /// </summary>
    [<Literal>]
    let Com = "Com"
