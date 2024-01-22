namespace Account.Domain

open System


type TransferInFinished =
    { AccountId: Guid
      Amount: decimal
      Balance: decimal }

    member me.Apply(agg: Transaction) = agg.Balance <- me.Balance


type TransferIn =
    { Amount: decimal }

    member me.Validate(agg: Transaction) =
        if agg.Limit = 0m then
            raise <| ValidateError "交易期间尚未生效"

        if me.Amount <= 0m then
            raise <| ValidateError "转入金额应大于零"

        if me.Amount > agg.TransLimit then
            raise <| ValidateError "金额超限"

    member me.Execute(agg: Transaction) =
        { AccountId = agg.AccountId
          Amount = me.Amount
          Balance = agg.Balance + me.Amount }
