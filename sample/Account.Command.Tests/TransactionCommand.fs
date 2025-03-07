[<AutoOpen>]
module Account.TestFixture.Transaction

open UniStream.Domain
open Account.Domain


let initPeriod agent aggId comId com =
    Aggregator.create<Transaction, InitPeriod, PeriodInited> agent aggId comId com

let openPeriod agent aggId comId com =
    Aggregator.create<Transaction, OpenPeriod, PeriodOpened> agent aggId comId com

let setLimit agent aggId comId com =
    Aggregator.apply<Transaction, SetLimit, LimitSetted> agent aggId comId com

let changeLimit agent aggId comId com =
    Aggregator.apply<Transaction, ChangeLimit, LimitChanged> agent aggId comId com

let setTransLimit agent aggId comId com =
    Aggregator.apply<Transaction, SetTransLimit, TransLimitSetted> agent aggId comId com

let deposit agent aggId comId com =
    Aggregator.apply<Transaction, Deposit, DepositFinished> agent aggId comId com

let withdraw agent aggId comId com =
    Aggregator.apply<Transaction, Withdraw, WithdrawFinished> agent aggId comId com

let transferOut agent aggId comId com =
    Aggregator.apply<Transaction, TransferOut, TransferOutFinished> agent aggId comId com

let transferIn agent aggId comId com =
    Aggregator.apply<Transaction, TransferIn, TransferInFinished> agent aggId comId com

let getTransaction agent aggId = Aggregator.get<Transaction> agent aggId
