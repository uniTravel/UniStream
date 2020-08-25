namespace UniStream.Domain

open System


/// <summary>配置模块
/// </summary>
[<RequireQualifiedAccess>]
module Config =

    /// <summary>不可变聚合配置
    /// </summary>
    [<Sealed>]
    type Immutable =

        /// <summary>构造函数
        /// </summary>
        /// <param name="esFunc">领域事件流存储函数。</param>
        /// <param name="ldFunc">领域日志流存储函数。</param>
        /// <param name="lgFunc">诊断日志流存储函数。</param>
        new :
            esFunc: (string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) *
            ldFunc: (string -> string -> ReadOnlyMemory<byte> -> Async<unit>) *
            lgFunc: (string -> ReadOnlyMemory<byte> -> Async<unit>) -> Immutable

        /// <summary>领域事件流存储函数
        /// </summary>
        /// <param name="aggType">带连字符‘-’的聚合类型。</param>
        /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
        /// <param name="version">事件版本。</param>
        /// <param name="eData">事件数据。</param>
        member EsFunc :
            aggType: string ->
            aggKey: string ->
            version: uint64 ->
            eData: (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq ->
            Async<unit>

        /// <summary>领域日志流存储函数
        /// </summary>
        /// <param name="user">用户。</param>
        /// <param name="category">领域日志类别。</param>
        /// <param name="data">领域日志数据。</param>
        member LdFunc :
            user: string ->
            category: string ->
            data: ReadOnlyMemory<byte> ->
            Async<unit>

        /// <summary>诊断日志流存储函数
        /// </summary>
        /// <param name="aggType">聚合类型。</param>
        /// <param name="data">诊断日志数据。</param>
        member LgFunc :
            aggType: string ->
            data: ReadOnlyMemory<byte> ->
            Async<unit>


    /// <summary>可变聚合配置
    /// <para>1、缓存与快照的容量应介于5000~100000之间。</para>
    /// <para>2、刷新缓存的间隔应介于10~180秒之间。</para>
    /// <para>3、清扫快照的间隔应介于24~72小时之间。</para>
    /// </summary>
    [<Sealed>]
    type Mutable =

        /// <summary>构造函数
        /// <para>刷新缓存间隔以秒为单位，清扫快照间隔以小时为单位。</para>
        /// </summary>
        /// <param name="get">从某个版本开始为聚合获取领域事件的函数。</param>
        /// <param name="esFunc">领域事件流存储函数。</param>
        /// <param name="ldFunc">领域日志流存储函数。</param>
        /// <param name="lgFunc">诊断日志流存储函数。</param>
        /// <param name="?capacity">缓存与快照的容量，缺省为10000。</param>
        /// <param name="?keep">清理缓存/快照后保留的数量，缺省为3000。</param>
        /// <param name="?refresh">刷新聚合缓存的间隔秒数，缺省为15秒。</param>
        /// <param name="?batch">批处理的间隔毫秒数，自然数表示启用/0表示不启用，缺省为0。</param>
        /// <param name="?scavenge">清扫聚合快照的间隔小时数，自然数表示启用/0表示不启用，缺省为0。</param>
        /// <param name="?threshold">快照间隔，缺省为1000。</param>
        new :
            get: (string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>) *
            esFunc: (string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) *
            ldFunc: (string -> string -> ReadOnlyMemory<byte> -> Async<unit>) *
            lgFunc: (string -> ReadOnlyMemory<byte> -> Async<unit>) *
            ?capacity: int *
            ?keep: int *
            ?refresh: uint *
            ?batch: uint *
            ?scavenge: uint *
            ?threshold: uint64 -> Mutable

        /// <summary>从某个版本开始为聚合获取领域事件的函数
        /// </summary>
        /// <param name="aggType">带连字符‘-’的聚合类型。</param>
        /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
        /// <param name="version">起始事件版本。</param>
        member Get :
            aggType: string ->
            aggKey: string ->
            version: uint64 ->
            Async<(uint64 * string * ReadOnlyMemory<byte>) seq>

        /// <summary>领域事件流存储函数
        /// </summary>
        /// <param name="aggType">带连字符‘-’的聚合类型。</param>
        /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
        /// <param name="version">事件版本。</param>
        /// <param name="eData">事件数据。</param>
        member EsFunc :
            aggType: string ->
            aggKey: string ->
            version: uint64 ->
            eData: (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq ->
            Async<unit>

        /// <summary>领域日志流存储函数
        /// </summary>
        /// <param name="user">用户。</param>
        /// <param name="category">领域日志类别。</param>
        /// <param name="data">领域日志数据。</param>
        member LdFunc :
            user: string ->
            category: string ->
            data: ReadOnlyMemory<byte> ->
            Async<unit>

        /// <summary>诊断日志流存储函数
        /// </summary>
        /// <param name="aggType">聚合类型。</param>
        /// <param name="data">诊断日志数据。</param>
        member LgFunc :
            aggType: string ->
            data: ReadOnlyMemory<byte> ->
            Async<unit>

        /// <summary>缓存与快照的容量
        /// </summary>
        member Capacity : int

        /// <summary>清理缓存/快照后保留的数量
        /// </summary>
        member Keep : int

        /// <summary>刷新聚合缓存的间隔秒数
        /// </summary>
        member Refresh : uint

        /// <summary>批处理的间隔毫秒数
        /// <para>自然数表示启用/0表示不启用。</para>
        /// </summary>
        member Batch : uint

        /// <summary>清扫聚合快照的间隔小时数
        /// <para>自然数表示启用/0表示不启用。</para>
        /// </summary>
        member Scavenge : uint

        /// <summary>快照间隔
        /// <para>自然数表示启用/否则表示不启用。</para>
        /// </summary>
        member Threshold : uint64


    /// <summary>观察者聚合配置
    /// <para>1、缓存与快照的容量应介于5000~100000之间。</para>
    /// <para>2、刷新缓存的间隔应介于30~3600分钟之间。</para>
    /// <para>3、清扫快照的间隔应介于24~72小时之间。</para>
    /// </summary>
    [<Sealed>]
    type Observer =

        /// <summary>构造函数
        /// </summary>
        /// <param name="get">从某个版本开始为聚合获取领域事件的函数。</param>
        /// <param name="lgFunc">诊断日志流存储函数。</param>
        /// <param name="?capacity">缓存与快照的容量，缺省为10000。</param>
        /// <param name="?keep">清理缓存/快照后保留的数量，缺省为5000。</param>
        /// <param name="?refresh">刷新聚合缓存的间隔分钟数，缺省为30分钟。</param>
        /// <param name="?scavenge">清扫聚合快照的间隔小时数，自然数表示启用/0表示不启用，缺省为0。</param>
        /// <param name="?threshold">快照间隔，缺省为1000。</param>
        new :
            get: (string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>) *
            lgFunc: (string -> ReadOnlyMemory<byte> -> Async<unit>) *
            ?capacity: int *
            ?keep: int *
            ?refresh: uint *
            ?scavenge: uint *
            ?threshold: uint64 -> Observer

        /// <summary>从某个版本开始为聚合获取领域事件的函数
        /// </summary>
        /// <param name="aggType">带连字符‘-’的聚合类型。</param>
        /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
        /// <param name="version">起始事件版本。</param>
        member Get :
            aggType: string ->
            aggKey: string ->
            version: uint64 ->
            Async<(uint64 * string * ReadOnlyMemory<byte>) seq>

        /// <summary>诊断日志流存储函数
        /// </summary>
        /// <param name="aggType">聚合类型。</param>
        /// <param name="data">诊断日志数据。</param>
        member LgFunc :
            aggType: string ->
            data: ReadOnlyMemory<byte> ->
            Async<unit>

        /// <summary>缓存与快照的容量
        /// </summary>
        member Capacity : int

        /// <summary>清理缓存/快照后保留的数量
        /// </summary>
        member Keep : int

        /// <summary>刷新聚合缓存的间隔秒数
        /// </summary>
        member Refresh : uint

        /// <summary>清扫聚合快照的间隔小时数
        /// <para>自然数表示启用/0表示不启用。</para>
        /// </summary>
        member Scavenge : uint

        /// <summary>快照间隔
        /// <para>自然数表示启用/否则表示不启用。</para>
        /// </summary>
        member Threshold : uint64