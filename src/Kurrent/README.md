# 简介

流式技术的 KurrentDB 实现。

> KurrentDB 是一款专为事件溯源 (Event Sourcing) 设计的数据库系统。它主要专注于存储和管理事件流，这使其非常适合那些需要保留完整历史记录和能够重播事件以重建系统状态的应用场景。KurrentDB 被设计用于高吞吐量、低延迟的环境，并且能够保证数据的持久性和一致性。


# 主要功能

* 管理 Kurrent 相关配置。
* 分布式聚合器的命令发送者、命令订阅者实现。


# 用法

> 与基于其他实现的应用只有细微差别，体现在：
> * 配置及预处理程序。
> * 命令订阅者注册聚合命令处理者的函数。

## 命令订阅者 BackgroundService

### 程序入口

```f#
namespace Account.Subscriber

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain
open Account.Application

module Program =

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder args

        builder.Services.AddSubscriber builder.Configuration |> ignore

        builder.Services
            .AddSubscriber<Account>(builder.Configuration)
            .AddHostedService<AccountWorker>()
            .AddSingleton<AccountService>()
        |> ignore

        builder.Services
            .AddSubscriber<Transaction>(builder.Configuration)
            .AddHostedService<TransactionWorker>()
            .AddSingleton<TransactionService>()
        |> ignore

        let app = builder.Build()

        using (app.Services.CreateScope()) (fun scope ->
            let services = scope.ServiceProvider
            services.GetRequiredService<AccountService>() |> ignore
            services.GetRequiredService<TransactionService>() |> ignore)

        app.Run()
        0 // exit code
```

### 后台任务

```f#
namespace Account.Subscriber

open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain
open Account.Application


type TransactionWorker
    (logger: ILogger<TransactionWorker>, client: IClient, subscriber: ISubscriber<Transaction>, svc: TransactionService)
    =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        Handler.register subscriber logger client ct svc.InitPeriod
        Handler.register subscriber logger client ct svc.OpenPeriod
        Handler.register subscriber logger client ct svc.SetLimit
        Handler.register subscriber logger client ct svc.ChangeLimit
        Handler.register subscriber logger client ct svc.SetTransLimit
        Handler.register subscriber logger client ct svc.Deposit
        Handler.register subscriber logger client ct svc.Withdraw
        Handler.register subscriber logger client ct svc.TransferOut
        Handler.register subscriber logger client ct svc.TransferIn
        subscriber.Launch ct
```

### 配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Kurrent": {
    "User": "admin",
    "Pass": "changeit",
    "Host": "kurrent-0.kurrent-headless.default.svc.cluster.local:2113,kurrent-1.kurrent-headless.default.svc.cluster.local:2113,kurrent-2.kurrent-headless.default.svc.cluster.local:2113",
    "VerifyCert": false
  },
  "Aggregate": {
    "Account": {
      "Capacity": 10000,
      "Multiple": 3
    },
    "Transaction": {
      "Capacity": 10000,
      "Multiple": 3
    }
  }
}
```

## 命令发送者 WebApi

### 程序入口

```f#
namespace Account.Sender

#nowarn "20"

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain


module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder args

        builder.Services.AddControllers()
        builder.Services.AddOpenApi()
        builder.Services.AddSender builder.Configuration

        builder.Services.AddSender<Account> builder.Configuration
        builder.Services.AddSender<Transaction> builder.Configuration

        let app = builder.Build()

        using (app.Services.CreateScope()) (fun scope ->
            let services = scope.ServiceProvider
            services.GetRequiredService<ISender<Account>>()
            services.GetRequiredService<ISender<Transaction>>())

        app.MapOpenApi()
        app.UseSwaggerUI(fun options -> options.SwaggerEndpoint("/openapi/v1.json", "v1"))

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
```

### 控制器

```f#
namespace Account.Sender.Controller

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain


[<ApiController>]
[<Route("[controller]/[action]/{comId}")>]
type TransactionController(logger: ILogger<TransactionController>, sender: ISender<Transaction>) as me =
    inherit ControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.InitPeriod(aggId: Guid, comId: Guid, com: InitPeriod) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.InitPeriod, result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.OpenPeriod(aggId: Guid, comId: Guid, com: OpenPeriod) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.OpenPeriod, result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.SetLimit(aggId: Guid, comId: Guid, com: SetLimit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.SetLimit, result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.ChangeLimit(aggId: Guid, comId: Guid, com: ChangeLimit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.ChangeLimit, result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.SetTransLimit(aggId: Guid, comId: Guid, com: SetTransLimit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.SetTransLimit, result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.Deposit(aggId: Guid, comId: Guid, com: Deposit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.Deposit, result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.Withdraw(aggId: Guid, comId: Guid, com: Withdraw) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.Withdraw, result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.TransferOut(aggId: Guid, comId: Guid, com: TransferOut) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.TransferOut, result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.TransferIn(aggId: Guid, comId: Guid, com: TransferIn) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.TransferIn, result)
        }
```

### 配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Kurrent": {
    "User": "admin",
    "Pass": "changeit",
    "Host": "kurrent-0.kurrent-headless.default.svc.cluster.local:2113,kurrent-1.kurrent-headless.default.svc.cluster.local:2113,kurrent-2.kurrent-headless.default.svc.cluster.local:2113",
    "VerifyCert": false
  },
  "Aggregate": {
    "Account": {
      "Capacity": 10000,
      "Multiple": 3
    },
    "Transaction": {
      "Capacity": 10000,
      "Multiple": 3
    }
  },
  "Command": {
    "Interval": 15
  },
  "AllowedHosts": "*"
}
```


# 其他注意事项

* 命令发送者每个节点独立聚合类型持久订阅。
* 命令订阅者多节点共享命令类型持久订阅。
