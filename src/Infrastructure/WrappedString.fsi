namespace UniStream.Infrastructure

module WrappedString =

    /// <summary>取值接口
    /// <para>获取包装的字符串值。</para>
    /// </summary>
    [<Interface>]
    type IWrappedString =
        abstract Value : string

    /// <summary>创建包装的字符串
    /// </summary>
    /// <param name="canonicalize">规范化字符串的函数。</param>
    /// <param name="isValid">数据验证函数。</param>
    /// <param name="ctor">构造字符串值的函数。</param>
    /// <returns>字符串。</returns>
    val create : (string -> 'a) -> ('a -> bool) -> ('a -> 'b) -> string -> 'b option

    /// <summary>将指定函数应用到字符串
    /// </summary>
    /// <param name="f">待应用的函数。</param>
    /// <param name="s">字符串。</param>
    val apply : (string -> 'a) -> IWrappedString -> 'a

    /// <summary>获取包装的字符串值
    /// </summary>
    /// <param name="s">字符串。</param>
    val value : IWrappedString -> string

    /// <summary>相等判断
    /// </summary>
    val equals : IWrappedString -> IWrappedString -> bool

    /// <summary>比较
    /// </summary>
    val compareTo : IWrappedString -> IWrappedString -> int

    /// <summary>规范为单行字符串
    /// </summary>
    val singleLineTrimmed : string -> string

    /// <summary>长度校验
    /// </summary>
    /// <param name="len">长度限制。</param>
    /// <param name="s">待校验的字符串。</param>
    val lengthValidator : int -> string -> bool

    /// <summary>长度100之内的字符串
    /// </summary>
    [<Sealed>]
    type String100 =
        interface IWrappedString

    /// <summary>创建长度100之内的字符串
    /// </summary>
    val string100 : (string -> String100 option)

    /// <summary>转换为长度100之内的字符串
    /// </summary>
    val convertTo100 : (IWrappedString -> String100 option)

    /// <summary>长度50之内的字符串
    /// </summary>
    [<Sealed>]
    type String50 =
        interface IWrappedString

    /// <summary>创建长度50之内的字符串
    /// </summary>
    val string50 : (string -> String50 option)

    /// <summary>转换为长度50之内的字符串
    /// </summary>
    val convertTo50 : (IWrappedString -> String50 option)

    /// <summary>添加到Map
    /// </summary>
    /// <param name="k">键。</param>
    /// <param name="v">键对应的值。</param>
    /// <param name="map">Map。</param>
    val mapAdd : IWrappedString -> 'a -> Map<string, 'a> -> Map<string, 'a>

    /// <summary>验证Map中是否存在
    /// </summary>
    /// <param name="k">键。</param>
    /// <param name="map">Map。</param>
    val mapContainsKey : IWrappedString -> Map<string, 'a> -> bool

    /// <summary>从Map获取
    /// </summary>
    /// <param name="k">键。</param>
    /// <param name="map">Map。</param>
    val mapTryFind : IWrappedString -> Map<string, 'a> -> 'a option


[<RequireQualifiedAccess>]
module EmailAddress =

    /// <summary>邮箱字符串
    /// </summary>
    [<Sealed>]
    type T =
        interface WrappedString.IWrappedString

    /// <summary>创建邮箱字符串
    /// </summary>
    val create : (string -> T option)

    /// <summary>转换为邮箱字符串
    /// </summary>
    val convert : (WrappedString.IWrappedString -> T option)