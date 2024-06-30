# 简介

UniStream 框架内核，处理聚合流。


# 主要功能

* 每个聚合类型一个聚合器实例，隔离不同聚合类型，便于并发操作。
* 每个聚合器分别缓存聚合当前状态，按配置的容量及操作顺序刷新缓存，保留最近有操作的聚合。
* 初始化需要注册重播，以便针对被清出本地缓存的聚合，从持久化的流存储重建相关聚合的本地缓存。

> 每个聚合区分创建与变更的函数。
> * 新建聚合须使用创建聚合函数。
> * 对聚合的后续命令须使用变更聚合函数。


# 用法

## 应用层

> 应用层依赖内核，与具体流式技术实现无关。

### 创建聚合类型

```f#
namespace Account.Domain

open UniStream.Domain


[<Sealed>]
type Account(id) =
    inherit Aggregate(id)

    member val Code = "" with get, set

    member val Owner = "" with get, set

    member val Limit = 0m with get, set

    member val VerifiedBy = "" with get, set

    member val Verified = false with get, set

    member val VerifyConclusion = false with get, set

    member val ApprovedBy = "" with get, set

    member val Approved = false with get, set
```

### 创建命令类型

```f#
namespace Account.Domain

open System.ComponentModel.DataAnnotations


type CreateAccount() =

    [<Required>]
    member val Owner = "" with get, set

    member me.Validate(agg: Account) = ()

    member me.Execute(agg: Account) = { Owner = me.Owner }


type VerifyAccount() =

    [<Required>]
    member val VerifiedBy = "" with get, set

    [<Required>]
    member val Conclusion = false with get, set

    member me.Validate(agg: Account) =
        if agg.Verified then
            let conclusion = if agg.VerifyConclusion then "审核通过" else "审核未通过"
            raise <| ValidateError $"已经审核，结论为：{conclusion}"

    member me.Execute(agg: Account) =
        { VerifiedBy = me.VerifiedBy
          Verified = true
          Conclusion = me.Conclusion }


type LimitAccount() =

    [<Required>]
    member val Limit = 0.0m with get, set

    member me.Validate(agg: Account) =
        if not agg.Approved then
            raise <| ValidateError "账户未批准"

        if me.Limit = agg.Limit then
            raise <| ValidateError "限额与原先一致"

        if me.Limit <= 0m then
            raise <| ValidateError "限额必须大于零"

    member me.Execute(agg: Account) =
        { AccountId = agg.Id; Limit = me.Limit }


type ApproveAccount() =

    [<Required>]
    member val ApprovedBy = "" with get, set

    [<Required>]
    member val Approved = false with get, set

    [<Required>]
    member val Limit = 0m with get, set

    member me.Validate(agg: Account) =
        if not agg.VerifyConclusion then
            raise <| ValidateError "未审核通过"

        if me.Approved && me.Limit <= 0m then
            raise <| ValidateError "批准的账户，限额必须大于零"

    member me.Execute(agg: Account) =
        { AccountId = agg.Id
          ApprovedBy = me.ApprovedBy
          Approved = me.Approved
          Limit = if me.Approved then me.Limit else 0m }
```

### 创建应用服务

```f#
namespace Account.Application

open Microsoft.Extensions.Options
open UniStream.Domain
open Account.Domain


type AccountService(stream: IStream<Account>, options: IOptionsMonitor<AggregateOptions>) =
    let options = options.Get(nameof Account)
    let agent = Aggregator.init Account stream options

    do
        Aggregator.register agent <| Replay<Account, AccountCreated>()
        Aggregator.register agent <| Replay<Account, AccountVerified>()
        Aggregator.register agent <| Replay<Account, AccountApproved>()
        Aggregator.register agent <| Replay<Account, AccountLimited>()

    /// <summary>创建账户
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新账户</returns>
    member _.CreateAccount aggId comId com =
        Aggregator.create<Account, CreateAccount, AccountCreated> agent aggId comId com

    /// <summary>审核账户
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>账户</returns>
    member _.VerifyAccount aggId comId com =
        Aggregator.apply<Account, VerifyAccount, AccountVerified> agent aggId comId com

    /// <summary>批准设立账户
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>账户</returns>
    member _.ApproveAccount aggId comId com =
        Aggregator.apply<Account, ApproveAccount, AccountApproved> agent aggId comId com

    /// <summary>设置账户限额
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>账户</returns>
    member _.LimitAccount aggId comId com =
        Aggregator.apply<Account, LimitAccount, AccountLimited> agent aggId comId com
```