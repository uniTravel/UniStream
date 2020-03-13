namespace UniStream.Domain

open System


/// <summary>元数据模块
/// </summary>
[<RequireQualifiedAccess>]
module MetaData =

    /// <summary>创建关联ID元数据
    /// </summary>
    /// <param name="id">关联ID。</param>
    val correlationId : Guid -> byte[]