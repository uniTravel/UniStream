namespace Account.Application

open System
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
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新交易期间</returns>
    member _.InitPeriod =
        Aggregator.create<Transaction, InitPeriod, PeriodInited> agent None

    /// <summary>滚动创建新交易期间
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新交易期间</returns>
    member _.OpenPeriod =
        Aggregator.create<Transaction, OpenPeriod, PeriodOpened> agent None

    /// <summary>滚动初始化限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.SetLimit = Aggregator.apply<Transaction, SetLimit, LimitSetted> agent None

    /// <summary>变更限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.ChangeLimit =
        Aggregator.apply<Transaction, ChangeLimit, LimitChanged> agent None

    /// <summary>设置交易限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.SetTransLimit =
        Aggregator.apply<Transaction, SetTransLimit, TransLimitSetted> agent None

    /// <summary>存款
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.Deposit = Aggregator.apply<Transaction, Deposit, DepositFinished> agent None

    /// <summary>取款
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.Withdraw =
        Aggregator.apply<Transaction, Withdraw, WithdrawFinished> agent None

    /// <summary>转出
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferOut(aggId: Guid, com: TransferOut) =
        Aggregator.apply<Transaction, TransferOut, TransferOutFinished> agent None aggId com

    /// <summary>转出
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferOut(traceId: Guid, aggId: Guid, com: TransferOut) =
        Aggregator.apply<Transaction, TransferOut, TransferOutFinished> agent (Some traceId) aggId com

    /// <summary>转入
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferIn(aggId: Guid, com: TransferIn) =
        Aggregator.apply<Transaction, TransferIn, TransferInFinished> agent None aggId com

    /// <summary>转入
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferIn(traceId: Guid, aggId: Guid, com: TransferIn) =
        Aggregator.apply<Transaction, TransferIn, TransferInFinished> agent (Some traceId) aggId com
