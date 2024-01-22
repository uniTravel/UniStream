namespace Account.Domain


type AccountVerified =
    { VerifiedBy: string
      Verified: bool
      Conclusion: bool }

    member me.Apply(agg: Account) =
        agg.VerifiedBy <- me.VerifiedBy
        agg.Verified <- me.Verified
        agg.VerifyConclusion <- me.Conclusion


type VerifyAccount =
    { VerifiedBy: string
      Conclusion: bool }

    member me.Validate(agg: Account) =
        if agg.Verified then
            let conclusion = if agg.VerifyConclusion then "审核通过" else "审核未通过"
            raise <| ValidateError $"已经审核，结论为：{conclusion}"

    member me.Execute(agg: Account) =
        { VerifiedBy = me.VerifiedBy
          Verified = true
          Conclusion = me.Conclusion }
