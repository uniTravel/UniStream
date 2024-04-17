namespace Account.Application

open Microsoft.Extensions.Options
open UniStream.Domain
open Account.Domain


type AccountService(stream: IStream, options: IOptionsMonitor<AggregateOptions>) =
    let options = options.Get(nameof Account)
    let agent = Aggregator.init Account stream options

    do
        Aggregator.register agent <| Replay<Account, AccountCreated>()
        Aggregator.register agent <| Replay<Account, AccountVerified>()
        Aggregator.register agent <| Replay<Account, AccountApproved>()
        Aggregator.register agent <| Replay<Account, AccountLimited>()

    /// <summary>创建账户
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新账户</returns>
    member _.CreateAccount traceId aggId com =
        Aggregator.create<Account, CreateAccount, AccountCreated> agent traceId aggId com

    /// <summary>审核账户
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>账户</returns>
    member _.VerifyAccount traceId aggId com =
        Aggregator.apply<Account, VerifyAccount, AccountVerified> agent traceId aggId com

    /// <summary>批准设立账户
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>账户</returns>
    member _.ApproveAccount traceId aggId com =
        Aggregator.apply<Account, ApproveAccount, AccountApproved> agent traceId aggId com

    /// <summary>设置账户限额
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>账户</returns>
    member _.LimitAccount traceId aggId com =
        Aggregator.apply<Account, LimitAccount, AccountLimited> agent traceId aggId com
