namespace UniStream.Domain


[<RequireQualifiedAccess>]
module Delta =

    /// <summary>边际影响序列化成UTF8字节数组
    /// <para>边际影响采用UTF8格式的Json序列化。</para>
    /// </summary>
    /// <typeparam name="'d">边际影响类型。</typeparam>
    /// <param name="delta">边际影响。</param>
    /// <returns>边际影响的UTF8字节数组。</returns>
    val inline internal asBytes : 'd -> byte[]

    /// <summary>UTF8字节数组反序列化成边际影响
    /// <para>边际影响采用UTF8格式的Json序列化。</para>
    /// </summary>
    /// <typeparam name="^d">边际影响类型。</typeparam>
    /// <param name="deltaBytes">边际影响的UTF8字节数组。</param>
    /// <returns>边际影响。</returns>
    val inline fromBytes : byte[] -> ^d