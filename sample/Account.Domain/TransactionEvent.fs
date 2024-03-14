namespace Account.Domain

open System


type PeriodInited =
    { AccountId: Guid
      Period: string
      Limit: decimal }

    member me.Apply(agg: Transaction) =
        agg.AccountId <- me.AccountId
        agg.Period <- me.Period
        agg.Limit <- me.Limit
        agg.TransLimit <- me.Limit


type PeriodOpened =
    { AccountId: Guid
      Period: string }

    member me.Apply(agg: Transaction) =
        agg.AccountId <- me.AccountId
        agg.Period <- me.Period


type LimitSetted =
    { Limit: decimal
      TransLimit: decimal
      Balance: decimal }

    member me.Apply(agg: Transaction) =
        agg.Limit <- me.Limit
        agg.TransLimit <- me.TransLimit
        agg.Balance <- me.Balance


type LimitChanged =
    { Limit: decimal
      TransLimit: decimal }

    member me.Apply(agg: Transaction) =
        agg.Limit <- me.Limit
        agg.TransLimit <- me.TransLimit


type TransLimitSetted =
    { TransLimit: decimal }

    member me.Apply(agg: Transaction) = agg.TransLimit <- me.TransLimit


type DepositFinished =
    { AccountId: Guid
      Amount: decimal
      Balance: decimal }

    member me.Apply(agg: Transaction) = agg.Balance <- me.Balance


type WithdrawFinished =
    { AccountId: Guid
      Amount: decimal
      Balance: decimal }

    member me.Apply(agg: Transaction) = agg.Balance <- me.Balance


type TransferOutFinished =
    { AccountId: Guid
      InCode: string
      Amount: decimal
      Balance: decimal }

    member me.Apply(agg: Transaction) = agg.Balance <- me.Balance


type TransferInFinished =
    { AccountId: Guid
      OutCode: string
      Amount: decimal
      Balance: decimal }

    member me.Apply(agg: Transaction) = agg.Balance <- me.Balance
