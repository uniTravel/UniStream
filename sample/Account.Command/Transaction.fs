namespace Account.Domain

open System
open System.ComponentModel.DataAnnotations
open UniStream.Domain


type InitPeriod() =

    [<ValidGuid>]
    member val AccountId = Guid.Empty with get, set

    [<Range(typeof<decimal>, "100", "100000", ErrorMessage = ValidateError.limit)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val Limit = 0m with get, set

    member me.Validate(agg: Transaction) = ()

    member me.Execute(agg: Transaction) =
        { AccountId = me.AccountId
          Period = $"{DateTime.Today:yyyyMM}"
          Limit = me.Limit }


type OpenPeriod() =

    [<ValidGuid>]
    member val AccountId = Guid.Empty with get, set

    member me.Validate(agg: Transaction) = ()

    member me.Execute(agg: Transaction) =
        { AccountId = me.AccountId
          Period = $"{DateTime.Today.AddMonths 1:yyyyMM}" }


type SetLimit() =

    [<Range(typeof<decimal>, "100", "100000", ErrorMessage = ValidateError.limit)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val Limit = 0m with get, set

    [<Range(typeof<decimal>, "100", "100000", ErrorMessage = ValidateError.transLimit)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val TransLimit = 0m with get, set

    [<Range(typeof<decimal>, "0", "79228162514264337593543950335", ErrorMessage = ValidateError.balance)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val Balance = 0m with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit <> 0m then
            raise <| ValidateException "不得重复设置限额"

    member me.Execute(agg: Transaction) =
        { Limit = me.Limit
          TransLimit = me.TransLimit
          Balance = me.Balance }

    interface IValidatableObject with
        member me.Validate(validationContext: ValidationContext) =
            [ if me.TransLimit > me.Limit then
                  yield ValidationResult ValidateError.limitTranslimit ]


type ChangeLimit() =

    [<Range(typeof<decimal>, "100", "100000", ErrorMessage = ValidateError.limit)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val Limit = 0m with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit = 0m then
            raise <| ValidateException "待修改限额须大于零"

    member me.Execute(agg: Transaction) =
        { Limit = me.Limit
          TransLimit =
            if agg.TransLimit > me.Limit then
                me.Limit
            else
                agg.TransLimit }


type SetTransLimit() =

    [<Range(typeof<decimal>, "100", "100000", ErrorMessage = ValidateError.transLimit)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val TransLimit = 0m with get, set

    member me.Validate(agg: Transaction) =
        if me.TransLimit > agg.Limit then
            raise <| ValidateException "交易限额不得超过控制限额"

    member me.Execute(agg: Transaction) = { TransLimit = me.TransLimit }


type Deposit() =

    [<Range(typeof<decimal>, "0.01", "79228162514264337593543950335", ErrorMessage = ValidateError.amount)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val Amount = 0m with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit = 0m then
            raise <| ValidateException "交易期间尚未生效"

        if me.Amount > agg.TransLimit then
            raise <| ValidateException "金额超限"

    member me.Execute(agg: Transaction) : DepositFinished =
        { AccountId = agg.AccountId
          Amount = me.Amount
          Balance = agg.Balance + me.Amount }


type Withdraw() =

    [<Range(typeof<decimal>, "0.01", "79228162514264337593543950335", ErrorMessage = ValidateError.amount)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val Amount = 0m with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit = 0m then
            raise <| ValidateException "交易期间尚未生效"

        if me.Amount > agg.Balance then
            raise <| ValidateException "余额不足"

        if me.Amount > agg.TransLimit then
            raise <| ValidateException "金额超限"

    member me.Execute(agg: Transaction) : WithdrawFinished =
        { AccountId = agg.AccountId
          Amount = me.Amount
          Balance = agg.Balance - me.Amount }


type TransferOut() =

    [<Range(typeof<decimal>, "0.01", "79228162514264337593543950335", ErrorMessage = ValidateError.amount)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val Amount = 0m with get, set

    [<Required(ErrorMessage = ValidateError.accountCode)>]
    member val InCode = "" with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit = 0m then
            raise <| ValidateException "交易期间尚未生效"

        if me.Amount > agg.Balance then
            raise <| ValidateException "余额不足"

        if me.Amount > agg.TransLimit then
            raise <| ValidateException "金额超限"

    member me.Execute(agg: Transaction) : TransferOutFinished =
        { AccountId = agg.AccountId
          InCode = me.InCode
          Amount = me.Amount
          Balance = agg.Balance - me.Amount }


type TransferIn() =

    [<Range(typeof<decimal>, "0.01", "79228162514264337593543950335", ErrorMessage = ValidateError.amount)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val Amount = 0m with get, set

    [<Required(ErrorMessage = ValidateError.accountCode)>]
    member val OutCode = "" with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit = 0m then
            raise <| ValidateException "交易期间尚未生效"

        if me.Amount > agg.TransLimit then
            raise <| ValidateException "金额超限"

    member me.Execute(agg: Transaction) : TransferInFinished =
        { AccountId = agg.AccountId
          OutCode = me.OutCode
          Amount = me.Amount
          Balance = agg.Balance + me.Amount }
