namespace UniStream.Domain

open System


/// <summary>边际影响模块
/// <para>边际影响包括领域命令值与领域事件值。</para>
/// </summary>
[<RequireQualifiedAccess>]
module Delta =

    /// <summary>边际影响序列化
    /// <para>边际影响采用UTF8格式的Json序列化。</para>
    /// </summary>
    /// <typeparam name="'d">边际影响类型。</typeparam>
    /// <param name="delta">边际影响。</param>
    val inline serialize: delta: 'd -> ReadOnlyMemory<byte>

    /// <summary>边际影响反序列化
    /// <para>边际影响采用UTF8格式的Json序列化。</para>
    /// </summary>
    /// <typeparam name="'d">边际影响类型。</typeparam>
    /// <param name="serialized">序列化的边际影响。</param>
    val inline deserialize: serialized: ReadOnlyMemory<byte> -> 'd
