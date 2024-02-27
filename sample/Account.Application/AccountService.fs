namespace Account.Application

open UniStream.Domain
open Account.Domain


type AccountService(writer, reader, capacity, refresh) =

    let agent = Aggregator.init Account writer reader capacity refresh

    do
        Aggregator.register agent <| Replay<Account, AccountCreated>()
        Aggregator.register agent <| Replay<Account, AccountVerified>()
        Aggregator.register agent <| Replay<Account, AccountApproved>()
        Aggregator.register agent <| Replay<Account, AccountLimited>()
        Aggregator.register agent <| Replay<Account, PeriodChanged>()

    /// <summary>创建账户
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新账户</returns>
    member _.CreateAccount =
        Aggregator.create<Account, CreateAccount, AccountCreated> agent None

    /// <summary>审核账户
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>账户</returns>
    member _.VerifyAccount =
        Aggregator.apply<Account, VerifyAccount, AccountVerified> agent None

    /// <summary>批准设立账户
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>账户</returns>
    member _.ApproveAccount =
        Aggregator.apply<Account, ApproveAccount, AccountApproved> agent None

    /// <summary>设置账户限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>账户</returns>
    member _.LimitAccount =
        Aggregator.apply<Account, LimitAccount, AccountLimited> agent None

    /// <summary>变更交易期间
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>账户</returns>
    member _.ChangePeriod =
        Aggregator.apply<Account, ChangePeriod, PeriodChanged> agent None
