namespace UniStream.Domain


[<RequireQualifiedAccess>]
module Cons =

    /// <summary>用于聚合相关的命名配置、键控服务
    /// </summary>
    [<Literal>]
    let Agg = "Aggregate"

    /// <summary>用于命令相关的命名配置、键控服务
    /// </summary>
    [<Literal>]
    let Com = "Command"
