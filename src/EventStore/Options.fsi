namespace UniStream.Infrastructure


/// <summary>配置选项
/// </summary>
[<Sealed>]
type Options =

    /// <summary>主构造函数
    /// </summary>
    new: unit -> Options

    /// <summary>配置节的名称
    /// </summary>
    static member Stream: string

    /// <summary>用户名
    /// </summary>
    member User: string with get, set

    /// <summary>密码
    /// </summary>
    member Pass: string with get, set

    /// <summary>host地址
    /// </summary>
    member Host: string with get, set

    /// <summary>是否验证证书
    /// </summary>
    member VerifyCert: bool with get, set

    /// <summary>聚合缓存容量
    /// </summary>
    member Capacity: int with get, set

    /// <summary>聚合缓存刷新间隔，单位秒
    /// </summary>
    member Refresh: float with get, set
