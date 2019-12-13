namespace UniStream.Infrastructure

open System


[<RequireQualifiedAccess>]
module EventStore =

    /// <summary>重建聚合
    /// <para>聚合类型全名作为Stream名称。</para>
    /// </summary>
    /// <param name="stream">Stream名称。</param>
    /// <param name="aId">聚合ID。</param>
    /// <returns>同一聚合ID下领域事件的有序集合，领域事件：元数据 * 数据。</returns>
    val getAgg : string -> Guid -> (byte[] * byte[])[]

    /// <summary>持久化领域事件
    /// <para>聚合类型全名作为Stream名称。</para>
    /// </summary>
    /// <param name="stream">Stream名称。</param>
    /// <param name="tId">跟踪ID。</param>
    /// <param name="delta">边际影响类型。</param>
    /// <param name="metaEvent">事件元数据，包括聚合ID、跟踪ID、事件类型、事件版本。</param>
    /// <param name="event">事件数据。</param>
    val esWrite : string -> Guid -> string -> byte[] -> byte[] -> unit

    /// <summary>持久化领域日志
    /// <para>聚合类型全名作为Stream名称。</para>
    /// </summary>
    /// <param name="stream">Stream名称。</param>
    /// <param name="tId">跟踪ID。</param>
    /// <param name="delta">边际影响类型。</param>
    /// <param name="metaTrace">追踪元数据，包括聚合ID、跟踪ID、事件类型。</param>
    /// <param name="dLog">领域日志数据。</param>
    val ldWrite : string -> Guid -> string -> byte[] -> byte[] -> unit

    /// <summary>持久化诊断日志
    /// <para>1、领域上下文作为Stream名称。</para>
    /// <para>2、元数据为空。</para>
    /// </summary>
    /// <param name="stream">Stream名称。</param>
    /// <param name="gLog">诊断日志数据。</param>
    val lgWrite : string -> byte[] -> unit