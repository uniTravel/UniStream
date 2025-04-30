namespace UniStream.Domain

open System.ComponentModel.DataAnnotations


/// <summary>Kurrent配置选项
/// </summary>
[<Sealed>]
type KurrentOptions =

    /// <summary>主构造函数
    /// </summary>
    new: unit -> KurrentOptions

    /// <summary>配置节的名称
    /// </summary>
    static member Name: string

    /// <summary>用户名
    /// </summary>
    [<Required>]
    member User: string with get, set

    /// <summary>密码
    /// </summary>
    [<Required>]
    member Pass: string with get, set

    /// <summary>host地址
    /// </summary>
    [<Required>]
    member Host: string with get, set

    /// <summary>是否验证证书
    /// </summary>
    member VerifyCert: bool with get, set
