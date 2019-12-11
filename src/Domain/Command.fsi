namespace UniStream.Domain


[<RequireQualifiedAccess>]
module Command =

    /// <summary>创建领域命令
    /// </summary>
    /// <typeparam name="'d">边际影响类型。</typeparam>
    /// <typeparam name="'c">领域命令类型。</typeparam>
    /// <param name="isValid">值验证函数。</param>
    /// <param name="ctor">构造领域命令的函数。</param>
    /// <param name="d">边际影响。</param>
    /// <returns>领域命令。</returns>
    val inline create : ('d -> bool) -> ('d -> 'c) -> 'd -> 'c

    /// <summary>应用函数
    /// <para>应用以领域命令作为参数的函数，返回相应结果。</para>
    /// </summary>
    /// <typeparam name="'d">边际影响类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <typeparam name="'a">应用函数返回值的类型。</typeparam>a
    /// <param name="f">待应用的函数。</param>e
    /// <param name="c">领域命令。</param>
    /// <returns>应用函数返回的结果。</returns>
    val inline apply : ('d -> 'a) -> ^c -> 'a
        when ^c : (member Value: 'd)

    /// <summary>转成UTF8字节数组
    /// <para>领域命令数据采用UTF8格式的Json序列化。</para>
    /// </summary>
    /// <typeparam name="'d">边际影响类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <param name="c">领域命令。</param>
    /// <returns>领域命令数据的UTF8字节数组。</returns>
    val inline asBytes : ^c -> byte[]
        when ^c : (member Value: 'd)