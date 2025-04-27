namespace Account.Application

open System
open System.Threading
open Microsoft.Extensions.Options
open UniStream.Domain
open Account.Domain


type AccountService(stream: IStream<Account>, options: IOptionsMonitor<AggregateOptions>) =
    let options = options.Get(nameof Account)
    let cts = new CancellationTokenSource()
    let agent = Aggregator.init Account cts.Token stream options
    let mutable dispose = false

    do
        Aggregator.register agent <| Replay<Account, AccountCreated>()
        Aggregator.register agent <| Replay<Account, AccountVerified>()
        Aggregator.register agent <| Replay<Account, AccountApproved>()
        Aggregator.register agent <| Replay<Account, AccountLimited>()

    /// <summary>创建账户
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>命令执行结果</returns>
    member _.CreateAccount aggId comId com =
        Aggregator.create<Account, CreateAccount, AccountCreated> agent aggId comId com

    /// <summary>审核账户
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>命令执行结果</returns>
    member _.VerifyAccount aggId comId com =
        Aggregator.apply<Account, VerifyAccount, AccountVerified> agent aggId comId com

    /// <summary>批准设立账户
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>命令执行结果</returns>
    member _.ApproveAccount aggId comId com =
        Aggregator.apply<Account, ApproveAccount, AccountApproved> agent aggId comId com

    /// <summary>设置账户限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>命令执行结果</returns>
    member _.LimitAccount aggId comId com =
        Aggregator.apply<Account, LimitAccount, AccountLimited> agent aggId comId com

    /// <summary>获取账户聚合
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <returns>账户聚合</returns>
    member internal _.Get aggId = Aggregator.get<Account> agent aggId

    interface IDisposable with
        member _.Dispose() =
            if not dispose then
                cts.Cancel()
                agent.Dispose()
                dispose <- true
