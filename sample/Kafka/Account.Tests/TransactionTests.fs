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
          svc.OpenPeriod id1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "打开但未生效的交易期间执行存款命令"
      <| fun _ ->
          let com = Deposit(Amount = 5m)

          let f =
              fun _ -> svc.Deposit id1 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "过账限额、余额以生效交易期间"
      <| fun _ ->
          let com = SetLimit(Limit = 1000m, TransLimit = 1000m, Balance = 94m)
          svc.SetLimit id1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "再次执行存款"
      <| fun _ ->
          let com = Deposit(Amount = 5m)
          svc.Deposit id1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "取款"
      <| fun _ ->
          let com = Withdraw(Amount = 1m)
          svc.Withdraw id1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "转入"
      <| fun _ ->
          let com = TransferIn(Amount = 100m)
          svc.TransferIn id1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "转出"
      <| fun _ ->
          let com = TransferOut(Amount = 10m)
          svc.TransferOut id1 (Guid.NewGuid()) com |> Async.RunSynchronously ]
    |> testList "打开新的交易期间"
    |> testSequenced
    |> testLabel "Transaction"


[<Tests>]
let test2 =
    [ testCase "初始化"
      <| fun _ ->
          let com =
              InitPeriod(AccountId = acid, Period = $"{DateTime.Today:yyyyMM}", Limit = 1000m)

          svc.InitPeriod id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "存款"
      <| fun _ ->
          let com = Deposit(Amount = 5m)
          svc.Deposit id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "取款"
      <| fun _ ->
          let com = Withdraw(Amount = 1m)
          svc.Withdraw id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "转入"
      <| fun _ ->
          let com = TransferIn(Amount = 100m)
          svc.TransferIn id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "转出"
      <| fun _ ->
          let com = TransferOut(Amount = 10m)
          svc.TransferOut id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "设置的交易限额超过账户限额"
      <| fun _ ->
          let com = SetTransLimit(TransLimit = 1050m)

          let f =
              fun _ -> svc.SetTransLimit id2 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "设置交易限额"
      <| fun _ ->
          let com = SetTransLimit(TransLimit = 500m)
          svc.SetTransLimit id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "变更的限额大于交易限额"
      <| fun _ ->
          let com = ChangeLimit(Limit = 700m)
          svc.ChangeLimit id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "变更的限额小于交易限额"
      <| fun _ ->
          let com = ChangeLimit(Limit = 300m)
          svc.ChangeLimit id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "取款金额超过余额"
      <| fun _ ->
          let com = Withdraw(Amount = 100m)

          let f =
              fun _ -> svc.Withdraw id2 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "转出金额超过余额"
      <| fun _ ->
          let com = TransferOut(Amount = 100m)

          let f =
              fun _ -> svc.TransferOut id2 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<ValidateError> f "异常类型有误" ]
    |> testList "初始创建的交易期间"
    |> testSequenced
    |> testLabel "Transaction"
