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
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新交易期间</returns>
    member _.InitPeriod aggId comId com =
        Aggregator.create<Transaction, InitPeriod, PeriodInited> agent aggId comId com

    /// <summary>滚动创建新交易期间
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新交易期间</returns>
    member _.OpenPeriod aggId comId com =
        Aggregator.create<Transaction, OpenPeriod, PeriodOpened> agent aggId comId com

    /// <summary>滚动初始化限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.SetLimit aggId comId com =
        Aggregator.apply<Transaction, SetLimit, LimitSetted> agent aggId comId com

    /// <summary>变更限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.ChangeLimit aggId comId com =
        Aggregator.apply<Transaction, ChangeLimit, LimitChanged> agent aggId comId com

    /// <summary>设置交易限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.SetTransLimit aggId comId com =
        Aggregator.apply<Transaction, SetTransLimit, TransLimitSetted> agent aggId comId com

    /// <summary>存款
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.Deposit aggId comId com =
        Aggregator.apply<Transaction, Deposit, DepositFinished> agent aggId comId com

    /// <summary>取款
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.Withdraw aggId comId com =
        Aggregator.apply<Transaction, Withdraw, WithdrawFinished> agent aggId comId com

    /// <summary>转出
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferOut aggId comId com =
        Aggregator.apply<Transaction, TransferOut, TransferOutFinished> agent aggId comId com

    /// <summary>转入
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>交易期间</returns>
    member _.TransferIn aggId comId com =
        Aggregator.apply<Transaction, TransferIn, TransferInFinished> agent aggId comId com
