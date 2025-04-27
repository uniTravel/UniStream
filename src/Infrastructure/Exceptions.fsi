namespace UniStream.Domain


/// <summary>验证异常
/// </summary>
/// <remarks>新建聚合或作用于聚合的命令，须执行逻辑验证。</remarks>
exception ValidateException of message: string


/// <summary>写入异常
/// </summary>
/// <remarks>写入流发生的异常，内部异常取决于具体流存储的实现。</remarks>
exception WriteException of message: string * inner: exn


/// <summary>读取异常
/// </summary>
/// <remarks>读取流发生的异常，内部异常取决于具体流存储的实现。</remarks>
exception ReadException of message: string * inner: exn


/// <summary>重播异常
/// </summary>
/// <remarks>重播事件流发生的异常，通常由于未注册相关重播。</remarks>
exception ReplayException of message: string * inner: exn


/// <summary>注册异常
/// </summary>
/// <remarks>未注册聚合命令代理。</remarks>
exception RegisterException of message: string
