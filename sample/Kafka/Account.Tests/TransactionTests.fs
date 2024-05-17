module Account.Tests.Transaction

open System
open Microsoft.Extensions.DependencyInjection
open Expecto
open Account.Domain
open Account.Application


let svc = host.Services.GetRequiredService<TransactionService>()
let id1 = Guid.NewGuid()
let id2 = Guid.NewGuid()
let acid = Guid.NewGuid()


[<Tests>]
let test1 =
    [ testCase "初始化"
      <| fun _ ->
          let next = DateTime.Today.AddMonths(1)
          let com = OpenPeriod(AccountId = acid, Period = $"{next:yyyyMM}")
          let agg = svc.OpenPeriod None id1 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 0m, 0m, 0m) "聚合值有误"
      testCase "打开但未生效的交易期间执行存款命令"
      <| fun _ ->
          let com = Deposit(Amount = 5m)
          let f = fun _ -> svc.Deposit None id1 com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "过账限额、余额以生效交易期间"
      <| fun _ ->
          let com = SetLimit(Limit = 1000m, TransLimit = 1000m, Balance = 94m)
          let agg = svc.SetLimit None id1 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 1000m, 1000m, 94m) "聚合值有误"
      testCase "再次执行存款"
      <| fun _ ->
          let com = Deposit(Amount = 5m)
          let agg = svc.Deposit None id1 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 1000m, 1000m, 99m) "聚合值有误"
      testCase "取款"
      <| fun _ ->
          let com = Withdraw(Amount = 1m)
          let agg = svc.Withdraw None id1 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 1000m, 1000m, 98m) "聚合值有误"
      testCase "转入"
      <| fun _ ->
          let com = TransferIn(Amount = 100m)
          let agg = svc.TransferIn None id1 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 1000m, 1000m, 198m) "聚合值有误"
      testCase "转出"
      <| fun _ ->
          let com = TransferOut(Amount = 10m)
          let agg = svc.TransferOut None id1 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 1000m, 1000m, 188m) "聚合值有误" ]
    |> testList "打开新的交易期间"
    |> testSequenced
    |> testLabel "Transaction"


[<Tests>]
let test2 =
    [ testCase "初始化"
      <| fun _ ->
          let com =
              InitPeriod(AccountId = acid, Period = $"{DateTime.Today:yyyyMM}", Limit = 1000m)

          let agg = svc.InitPeriod None id2 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 1000m, 1000m, 0m) "聚合值有误"
      testCase "存款"
      <| fun _ ->
          let com = Deposit(Amount = 5m)
          let agg = svc.Deposit None id2 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 1000m, 1000m, 5m) "聚合值有误"
      testCase "取款"
      <| fun _ ->
          let com = Withdraw(Amount = 1m)
          let agg = svc.Withdraw None id2 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 1000m, 1000m, 4m) "聚合值有误"
      testCase "转入"
      <| fun _ ->
          let com = TransferIn(Amount = 100m)
          let agg = svc.TransferIn None id2 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 1000m, 1000m, 104m) "聚合值有误"
      testCase "转出"
      <| fun _ ->
          let com = TransferOut(Amount = 10m)
          let agg = svc.TransferOut None id2 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 1000m, 1000m, 94m) "聚合值有误"
      testCase "设置的交易限额超过账户限额"
      <| fun _ ->
          let com = SetTransLimit(TransLimit = 1050m)
          let f = fun _ -> svc.SetTransLimit None id2 com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "设置交易限额"
      <| fun _ ->
          let com = SetTransLimit(TransLimit = 500m)
          let agg = svc.SetTransLimit None id2 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 1000m, 500m, 94m) "聚合值有误"
      testCase "变更的限额大于交易限额"
      <| fun _ ->
          let com = ChangeLimit(Limit = 700m)
          let agg = svc.ChangeLimit None id2 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 700m, 500m, 94m) "聚合值有误"
      testCase "变更的限额小于交易限额"
      <| fun _ ->
          let com = ChangeLimit(Limit = 300m)
          let agg = svc.ChangeLimit None id2 com |> Async.RunSynchronously
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (acid, 300m, 300m, 94m) "聚合值有误"
      testCase "取款金额超过余额"
      <| fun _ ->
          let com = Withdraw(Amount = 100m)
          let f = fun _ -> svc.Withdraw None id2 com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "转出金额超过余额"
      <| fun _ ->
          let com = TransferOut(Amount = 100m)
          let f = fun _ -> svc.TransferOut None id2 com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "异常类型有误" ]
    |> testList "初始创建的交易期间"
    |> testSequenced
    |> testLabel "Transaction"
