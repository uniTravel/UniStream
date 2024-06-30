# 简介

流式技术的Kafka实现。

> Apache Kafka 是一个开源的流处理平台，由 LinkedIn 开发并贡献给 Apache 软件基金会，现在已经成为一个顶级项目。Kafka 主要设计用于构建实时数据管道和流应用，它能够以高吞吐量、低延迟的方式处理大量数据流。


# 主要功能

* 管理 Kafka 相关配置。
* 单节点聚合器的流式实现。
* 分布式聚合器的命令发送者、命令订阅者实现。


# 用法

> 与基于其他实现的应用只有细微差别，体现在：
> * 配置及预处理程序。
> * 命令订阅者注册聚合命令处理者的函数。

## 单节点 WebApi

### 程序入口

```f#
namespace Account.Handler

#nowarn "20"

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain
open Account.Application


module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()
        builder.Services.AddEndpointsApiExplorer()
        builder.Services.AddSwaggerGen()
        builder.Services.AddHandler(builder.Configuration)

        builder.Services
            .AddHandler<Account>(builder.Configuration)
            .AddSingleton<AccountService>()

        builder.Services
            .AddHandler<Transaction>(builder.Configuration)
            .AddSingleton<TransactionService>()

        let app = builder.Build()

        using (app.Services.CreateScope()) (fun scope ->
            let services = scope.ServiceProvider
            services.GetRequiredService<AccountService>()
            services.GetRequiredService<TransactionService>())

        if app.Environment.IsDevelopment() then
            app.UseSwagger()
            app.UseSwaggerUI()
            ()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
```

### 控制器

```f#
namespace Account.Api.Controllers

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Account.Application
open Account.Domain


[<ApiController>]
[<Route("[controller]/[action]/{comId}")>]
type AccountController(logger: ILogger<AccountController>, svc: AccountService) as me =
    inherit ControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.CreateAccount(aggId: Guid, comId: Guid, com: CreateAccount) =
        task {
            let! result = svc.CreateAccount aggId comId com
            return me.CreatedAtAction(nameof (me.CreateAccount), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.VerifyAccount(aggId: Guid, comId: Guid, com: VerifyAccount) =
        task {
            let! result = svc.VerifyAccount aggId comId com
            return me.CreatedAtAction(nameof (me.VerifyAccount), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.ApproveAccount(aggId: Guid, comId: Guid, com: ApproveAccount) =
        task {
            let! result = svc.ApproveAccount aggId comId com
            return me.CreatedAtAction(nameof (me.ApproveAccount), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.LimitAccount(aggId: Guid, comId: Guid, com: LimitAccount) =
        task {
            let! result = svc.LimitAccount aggId comId com
            return me.CreatedAtAction(nameof (me.LimitAccount), result)
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
  "Kafka": {
    "Typ": {
      "Producer": {
        "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392"
      },
      "Consumer": {
        "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392",
        "AutoOffsetReset": 1,
        "GroupId": "account.handler1"
      }
    },
    "Agg": {
      "Consumer": {
        "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392",
        "EnableAutoCommit": false,
        "AutoOffsetReset": 1,
        "EnablePartitionEof": true
      }
    },
    "Admin": {
      "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392"
    }
  },
  "Aggregate": {
    "Account": {
      "Capacity": 3,
      "Multiple": 2
    },
    "Transaction": {
      "Capacity": 3,
      "Multiple": 2
    }
  },
  "AllowedHosts": "*"
}
```

## 分布式

### 命令订阅者 BackgroundService

#### 程序入口

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

        let builder = Host.CreateApplicationBuilder(args)

        builder.Services.AddSubscriber(builder.Configuration) |> ignore

        builder.Services
            .AddSubscriber<Transaction>(builder.Configuration)
            .AddHostedService<TransactionWorker>()
            .AddSingleton<TransactionService>()
        |> ignore

        let app = builder.Build()

        using (app.Services.CreateScope()) (fun scope ->
            let services = scope.ServiceProvider
            services.GetRequiredService<TransactionService>() |> ignore)

        app.Run()

        0 // exit code
```

#### 后台任务

```f#
namespace Account.Subscriber

open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain
open Account.Application


type TransactionWorker
    (
        logger: ILogger<TransactionWorker>,
        subscriber: ISubscriber<Transaction>,
        [<FromKeyedServices(Cons.Typ)>] producer: IProducer,
        svc: TransactionService
    ) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        Handler.register subscriber logger producer svc.InitPeriod
        Handler.register subscriber logger producer svc.OpenPeriod
        Handler.register subscriber logger producer svc.SetLimit
        Handler.register subscriber logger producer svc.ChangeLimit
        Handler.register subscriber logger producer svc.SetTransLimit
        Handler.register subscriber logger producer svc.Deposit
        Handler.register subscriber logger producer svc.Withdraw
        Handler.register subscriber logger producer svc.TransferOut
        Handler.register subscriber logger producer svc.TransferIn
        subscriber.Launch ct
```

#### 配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Kafka": {
    "Com": {
      "Consumer": {
        "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392",
        "GroupId": "account.subscriber"
      }
    },
    "Typ": {
      "Producer": {
        "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392"
      },
      "Consumer": {
        "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392",
        "AutoOffsetReset": 1,
        "GroupId": "account.subscriber1"
      }
    },
    "Agg": {
      "Consumer": {
        "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392",
        "EnableAutoCommit": false,
        "AutoOffsetReset": 1,
        "EnablePartitionEof": true
      }
    },
    "Admin": {
      "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392"
    }
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

### 命令发送者 WebApi

#### 程序入口

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

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()
        builder.Services.AddSender(builder.Configuration)

        builder.Services.AddSender<Transaction>(builder.Configuration)

        let app = builder.Build()

        using (app.Services.CreateScope()) (fun scope ->
            let services = scope.ServiceProvider
            services.GetRequiredService<ISender<Transaction>>())

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
```

#### 控制器

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
            return me.CreatedAtAction(nameof (me.InitPeriod), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.OpenPeriod(aggId: Guid, comId: Guid, com: OpenPeriod) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.OpenPeriod), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.SetLimit(aggId: Guid, comId: Guid, com: SetLimit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.SetLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.ChangeLimit(aggId: Guid, comId: Guid, com: ChangeLimit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.ChangeLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.SetTransLimit(aggId: Guid, comId: Guid, com: SetTransLimit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.SetTransLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.Deposit(aggId: Guid, comId: Guid, com: Deposit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.Deposit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.Withdraw(aggId: Guid, comId: Guid, com: Withdraw) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.Withdraw), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.TransferOut(aggId: Guid, comId: Guid, com: TransferOut) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.TransferOut), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.TransferIn(aggId: Guid, comId: Guid, com: TransferIn) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.TransferIn), result)
        }
```

#### 配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Kafka": {
    "Com": {
      "Producer": {
        "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392"
      }
    },
    "Typ": {
      "Consumer": {
        "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392",
        "AutoOffsetReset": 1,
        "GroupId": "account.sender1"
      }
    },
    "Admin": {
      "BootstrapServers": "localhost:9192,localhost:9292,localhost:9392"
    }
  },
  "Command": {
    "Interval": 15
  },
  "AllowedHosts": "*"
}
```


# 其他注意事项

* 需要预先运行投影程序 (后台任务)。
* 命令发送者每个节点独立聚合类型消费组。
* 命令订阅者每个节点独立聚合类型消费组。
* 投影程序多节点共享聚合类型消费组。