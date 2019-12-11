namespace UniStream.Domain


[<RequireQualifiedAccess>]
module MetaEvent =

    /// <summary>领域事件元数据
    /// </summary>
    type T

    /// <summary>创建领域事件元数据
    /// </summary>
    /// <param name="metaTrace">领域追踪元数据。</param>
    /// <param name="version">事件版本。</param>
    /// <returns>领域事件元数据。</returns>
    val internal create : MetaTrace.T -> int -> T

    /// <summary>转成字节数组
    /// <para>领域事件元数据采用二进制序列化。</para>
    /// </summary>
    /// <param name="metaEvent">领域事件元数据。</param>
    /// <returns>领域事件元数据的字节数组。</returns>
    val internal asBytes : T -> byte[]

    /// <summary>转成领域事件元数据
    /// <para>领域事件元数据采用二进制反序列化。</para>
    /// </summary>
    /// <param name="bytes">领域事件元数据的字节数组。</param>
    /// <returns>领域事件元数据。</returns>
    val internal fromBytes : byte[] -> T