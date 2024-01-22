namespace Account.Application

open UniStream.Domain
open Account.Domain


type TransactionService(writer, reader, capacity, refresh) =

    let agent = Aggregator.init Transaction writer reader capacity refresh

    do
        Aggregator.register agent <| Replay<Transaction, PeriodInited>()
        Aggregator.register agent <| Replay<Transaction, PeriodOpened>()
        Aggregator.register agent <| Replay<Transaction, LimitSetted>()
        Aggregator.register agent <| Replay<Transaction, LimitChanged>()
        Aggregator.register agent <| Replay<Transaction, TransLimitSetted>()
        Aggregator.register agent <| Replay<Transaction, DepositFinished>()
        Aggregator.register agent <| Replay<Transaction, WithdrawFinished>()
        Aggregator.register agent <| Replay<Transaction, TransferInFinished>()
        Aggregator.register agent <| Replay<Transaction, TransferOutFinished>()

    /// <summary>初始创建交易期间
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新交易期间</returns>
    member _.InitPeriod = Aggregator.create<Transaction, InitPeriod, PeriodInited> agent

    /// <summary>滚动创建新交易期间
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新交易期间</returns>
    member _.OpenPeriod = Aggregator.create<Transaction, OpenPeriod, PeriodOpened> agent

    /// <summary>滚动初始化限额
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.SetLimit = Aggregator.apply<Transaction, SetLimit, LimitSetted> agent

    /// <summary>变更限额
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.ChangeLimit = Aggregator.apply<Transaction, ChangeLimit, LimitChanged> agent

    /// <summary>设置交易限额
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.SetTransLimit =
        Aggregator.apply<Transaction, SetTransLimit, TransLimitSetted> agent

    /// <summary>存款
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.Deposit = Aggregator.apply<Transaction, Deposit, DepositFinished> agent

    /// <summary>取款
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.Withdraw = Aggregator.apply<Transaction, Withdraw, WithdrawFinished> agent

    /// <summary>转出
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferOut =
        Aggregator.apply<Transaction, TransferOut, TransferOutFinished> agent

    /// <summary>转入
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferIn =
        Aggregator.apply<Transaction, TransferIn, TransferInFinished> agent
