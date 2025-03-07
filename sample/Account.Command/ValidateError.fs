namespace Account.Domain


[<RequireQualifiedAccess>]
module ValidateError =

    [<Literal>]
    let guid = "无效的Guid格式或值为空"

    [<Literal>]
    let owner = "账户所有人不能为空"

    [<Literal>]
    let verifiedBy = "审核人不能为空"

    [<Literal>]
    let approvedBy = "审批人不能为空"

    [<Literal>]
    let approvedLimit = "批准的账户，限额必须大于零"

    [<Literal>]
    let money = "金额不得为负，且最多允许两位小数"

    [<Literal>]
    let limit = "限额必须在100到100000之间"

    [<Literal>]
    let transLimit = "交易限额必须在100到100000之间"

    [<Literal>]
    let limitTranslimit = "交易限额不得超过控制限额"

    [<Literal>]
    let balance = "余额必须大于等于零"

    [<Literal>]
    let amount = "交易金额必须大于等于零"

    [<Literal>]
    let accountCode = "账户代码不能为空"
