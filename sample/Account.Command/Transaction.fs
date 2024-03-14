namespace Account.Domain

open System
open System.ComponentModel.DataAnnotations


type InitPeriod() =

    [<Required>]
    member val AccountId = Guid.Empty with get, set

    [<StringLength(6)>]
    member val Period = "" with get, set

    [<Required>]
    member val Limit = 0m with get, set

    member me.Validate(agg: Transaction) =
        let period = $"{DateTime.Today:yyyyMM}"

        if me.Period <> period then
            raise <| ValidateError $"交易期间应为{period}，而实际传入为{me.Period}"

    member me.Execute(agg: Transaction) =
        { AccountId = me.AccountId
          Period = me.Period
          Limit = me.Limit }


type OpenPeriod() =

    [<Required>]
    member val AccountId = Guid.Empty with get, set

    [<StringLength(6)>]
    member val Period = "" with get, set

    member me.Validate(agg: Transaction) =
        let next = DateTime.Today.AddMonths(1)
        let period = $"{next:yyyyMM}"

        if me.Period <> period then
            raise <| ValidateError $"交易期间应为{period}，而实际传入为{me.Period}"

    member me.Execute(agg: Transaction) =
        { AccountId = me.AccountId
          Period = me.Period }


type SetLimit() =

    [<Required>]
    member val Limit = 0m with get, set

    [<Required>]
    member val TransLimit = 0m with get, set

    [<Required>]
    member val Balance = 0m with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit <> 0m then
            raise <| ValidateError "不得重复设置限额"

    member me.Execute(agg: Transaction) =
        { Limit = me.Limit
          TransLimit = me.TransLimit
          Balance = me.Balance }


type ChangeLimit() =

    [<Required>]
    member val Limit = 0m with get, set

    member me.Validate(agg: Transaction) = ()

    member me.Execute(agg: Transaction) =
        { Limit = me.Limit
          TransLimit =
            if agg.TransLimit > me.Limit then
                me.Limit
            else
                agg.TransLimit }


type SetTransLimit() =

    [<Required>]
    member val TransLimit = 0m with get, set

    member me.Validate(agg: Transaction) =
        if me.TransLimit > agg.Limit then
            raise <| ValidateError "交易限额不得超过控制限额"

    member me.Execute(agg: Transaction) = { TransLimit = me.TransLimit }


type Deposit() =

    [<Required>]
    member val Amount = 0m with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit = 0m then
            raise <| ValidateError "交易期间尚未生效"

        if me.Amount <= 0m then
            raise <| ValidateError "存入金额应大于零"

        if me.Amount > agg.TransLimit then
            raise <| ValidateError "金额超限"

    member me.Execute(agg: Transaction) : DepositFinished =
        { AccountId = agg.AccountId
          Amount = me.Amount
          Balance = agg.Balance + me.Amount }


type Withdraw() =

    [<Required>]
    member val Amount = 0m with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit = 0m then
            raise <| ValidateError "交易期间尚未生效"

        if me.Amount <= 0m then
            raise <| ValidateError "取出金额应大于零"

        if me.Amount > agg.Balance then
            raise <| ValidateError "余额不足"

        if me.Amount > agg.TransLimit then
            raise <| ValidateError "金额超限"

    member me.Execute(agg: Transaction) : WithdrawFinished =
        { AccountId = agg.AccountId
          Amount = me.Amount
          Balance = agg.Balance - me.Amount }


type TransferOut() =

    [<Required>]
    member val Amount = 0m with get, set

    [<Required>]
    member val InCode = "" with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit = 0m then
            raise <| ValidateError "交易期间尚未生效"

        if me.Amount <= 0m then
            raise <| ValidateError "转出金额应大于零"

        if me.Amount > agg.Balance then
            raise <| ValidateError "余额不足"

        if me.Amount > agg.TransLimit then
            raise <| ValidateError "金额超限"

    member me.Execute(agg: Transaction) : TransferOutFinished =
        { AccountId = agg.AccountId
          InCode = me.InCode
          Amount = me.Amount
          Balance = agg.Balance - me.Amount }


type TransferIn() =

    [<Required>]
    member val Amount = 0m with get, set

    [<Required>]
    member val OutCode = "" with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit = 0m then
            raise <| ValidateError "交易期间尚未生效"

        if me.Amount <= 0m then
            raise <| ValidateError "转入金额应大于零"

        if me.Amount > agg.TransLimit then
            raise <| ValidateError "金额超限"

    member me.Execute(agg: Transaction) : TransferInFinished =
        { AccountId = agg.AccountId
          OutCode = me.OutCode
          Amount = me.Amount
          Balance = agg.Balance + me.Amount }
