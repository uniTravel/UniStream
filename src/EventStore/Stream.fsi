namespace UniStream.Domain


/// <summary>Stream类型
/// </summary>
[<Sealed>]
type Stream =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    new: client: IClient -> Stream

    interface IStream
