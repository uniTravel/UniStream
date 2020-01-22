namespace UniStream.Domain


/// <summary>领域命令模块
/// </summary>
[<RequireQualifiedAccess>]
module Command =

    /// <summary>创建领域命令
    /// </summary>
    /// <typeparam name="'d">边际影响类型。</typeparam>
    /// <typeparam name="'c">领域命令类型。</typeparam>
    /// <param name="isValid">值验证函数。</param>
    /// <param name="ctor">构造领域命令的函数。</param>
    /// <param name="delta">边际影响。</param>
    /// <returns>领域命令。</returns>
    val create : ('d -> bool) -> ('d -> 'c) -> 'd -> 'c