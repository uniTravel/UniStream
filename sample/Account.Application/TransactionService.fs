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
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新交易期间</returns>
    member _.InitPeriod aggId com =
        Aggregator.create<Transaction, InitPeriod, PeriodInited> agent None aggId com

    /// <summary>滚动创建新交易期间
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新交易期间</returns>
    member _.OpenPeriod aggId com =
        Aggregator.create<Transaction, OpenPeriod, PeriodOpened> agent None aggId com

    /// <summary>滚动初始化限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.SetLimit aggId com =
        Aggregator.apply<Transaction, SetLimit, LimitSetted> agent None aggId com

    /// <summary>变更限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.ChangeLimit aggId com =
        Aggregator.apply<Transaction, ChangeLimit, LimitChanged> agent None aggId com

    /// <summary>设置交易限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.SetTransLimit aggId com =
        Aggregator.apply<Transaction, SetTransLimit, TransLimitSetted> agent None aggId com

    /// <summary>存款
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.Deposit aggId com =
        Aggregator.apply<Transaction, Deposit, DepositFinished> agent None aggId com

    /// <summary>取款
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.Withdraw aggId com =
        Aggregator.apply<Transaction, Withdraw, WithdrawFinished> agent None aggId com

    /// <summary>转出
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferOut traceId aggId com =
        Aggregator.apply<Transaction, TransferOut, TransferOutFinished> agent (Some traceId) aggId com

    /// <summary>转入
    /// </summary>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferIn traceId aggId com =
        Aggregator.apply<Transaction, TransferIn, TransferInFinished> agent (Some traceId) aggId com
