namespace UniStream.Domain


/// <summary>元数据模块
/// </summary>
[<RequireQualifiedAccess>]
module MetaData =

    /// <summary>创建关联ID元数据
    /// </summary>
    /// <param name="id">聚合ID或关联Key。</param>
    val correlationId : string -> byte[]