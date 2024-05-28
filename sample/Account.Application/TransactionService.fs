namespace Account.Application

open Microsoft.Extensions.Options
open UniStream.Domain
open Account.Domain


type TransactionService(stream: IStream, options: IOptionsMonitor<AggregateOptions>) =
    let options = options.Get(nameof Transaction)
    let agent = Aggregator.init Transaction stream options

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
        Aggregator.create<Transaction, InitPeriod, PeriodInited> agent aggId com

    /// <summary>滚动创建新交易期间
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新交易期间</returns>
    member _.OpenPeriod aggId com =
        Aggregator.create<Transaction, OpenPeriod, PeriodOpened> agent aggId com

    /// <summary>滚动初始化限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.SetLimit aggId com =
        Aggregator.apply<Transaction, SetLimit, LimitSetted> agent aggId com

    /// <summary>变更限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.ChangeLimit aggId com =
        Aggregator.apply<Transaction, ChangeLimit, LimitChanged> agent aggId com

    /// <summary>设置交易限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.SetTransLimit aggId com =
        Aggregator.apply<Transaction, SetTransLimit, TransLimitSetted> agent aggId com

    /// <summary>存款
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.Deposit aggId com =
        Aggregator.apply<Transaction, Deposit, DepositFinished> agent aggId com

    /// <summary>取款
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.Withdraw aggId com =
        Aggregator.apply<Transaction, Withdraw, WithdrawFinished> agent aggId com

    /// <summary>转出
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferOut aggId com =
        Aggregator.apply<Transaction, TransferOut, TransferOutFinished> agent aggId com

    /// <summary>转入
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferIn aggId com =
        Aggregator.apply<Transaction, TransferIn, TransferInFinished> agent aggId com
