module Account.Tests.Transaction

open System
open Microsoft.Extensions.DependencyInjection
open Expecto
open UniStream.Domain
open Account.Domain
open Account.Application
open Account.TestFixture


let svc = provider.GetRequiredService<TransactionService>()

[<Tests>]
let test1 =
    let aggId = Guid.NewGuid()
    let accountId = Guid.NewGuid()

    [ testAsync "余额为零：初始化交易期间" {
          let com = InitPeriod(AccountId = accountId, Limit = 10000m)
          let! r = svc.InitPeriod aggId (Guid.NewGuid()) com
          Expect.equal r Success "命令返回结果异常"
          let! agg = svc.Get aggId
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (accountId, 10000m, 10000m, 0m) "聚合数据有误"
      } ]
    |> testList "余额为零"
    |> testSequenced
    |> testLabel "Transaction"

[<Tests>]
let test2 =
    let buildTest setup =
        [ test "期间生效：存入" {
              setup
              <| fun aggId ->
                  async {
                      let com = Deposit(Amount = 500m)
                      let! r = svc.Deposit aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (10000m, 10000m, 500m) "聚合数据有误"
                  }
          }
          test "期间生效：转入" {
              setup
              <| fun aggId ->
                  async {
                      let com = TransferIn(Amount = 1000m, OutCode = "123456")
                      let! r = svc.TransferIn aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (10000m, 10000m, 1000m) "聚合数据有误"
                  }
          }
          test "余额为零：变更限额，不变" {
              setup
              <| fun aggId ->
                  async {
                      let com = ChangeLimit(Limit = 10000m)
                      let! r = svc.ChangeLimit aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (10000m, 10000m, 0m) "聚合数据有误"
                  }
          }
          test "余额为零：变更限额，变大" {
              setup
              <| fun aggId ->
                  async {
                      let com = ChangeLimit(Limit = 20000m)
                      let! r = svc.ChangeLimit aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (20000m, 10000m, 0m) "聚合数据有误"
                  }
          }
          test "余额为零：变更限额，变小" {
              setup
              <| fun aggId ->
                  async {
                      let com = ChangeLimit(Limit = 5000m)
                      let! r = svc.ChangeLimit aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (5000m, 5000m, 0m) "聚合数据有误"
                  }
          }
          test "余额为零：变更交易限额" {
              setup
              <| fun aggId ->
                  async {
                      let com = SetTransLimit(TransLimit = 7000m)
                      let! r = svc.SetTransLimit aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (10000m, 7000m, 0m) "聚合数据有误"
                  }
          } ]
        |> testList "余额为零"
        |> testLabel "Transaction"

    buildTest
    <| fun f ->
        let aggId = Guid.NewGuid()
        let com = InitPeriod(AccountId = Guid.NewGuid(), Limit = 10000m)
        let _ = svc.InitPeriod aggId (Guid.NewGuid()) com |> Async.RunSynchronously
        f aggId |> Async.RunSynchronously

[<Tests>]
let test3 =
    let aggId = Guid.NewGuid()
    let accountId = Guid.NewGuid()

    [ testAsync "期间未生效：打开期间" {
          let com = OpenPeriod(AccountId = accountId)
          let! r = svc.OpenPeriod aggId (Guid.NewGuid()) com
          Expect.equal r Success "命令返回结果异常"
          let! agg = svc.Get aggId
          Expect.equal (agg.AccountId, agg.Limit, agg.TransLimit, agg.Balance) (accountId, 0m, 0m, 0m) "聚合数据有误"
      }
      testAsync "期间生效：设置限额" {
          let com = SetLimit(Limit = 10000m, TransLimit = 10000m, Balance = 500m)
          let! r = svc.SetLimit aggId (Guid.NewGuid()) com
          Expect.equal r Success "命令返回结果异常"
          let! agg = svc.Get aggId
          Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (10000m, 10000m, 500m) "聚合数据有误"
      } ]
    |> testList "期间生效"
    |> testSequenced
    |> testLabel "Transaction"

[<Tests>]
let test4 =
    let buildTest setup =
        [ test "期间生效：存入" {
              setup
              <| fun aggId ->
                  async {
                      let com = Deposit(Amount = 500m)
                      let! r = svc.Deposit aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (10000m, 10000m, 1000m) "聚合数据有误"
                  }
          }
          test "期间生效：取出" {
              setup
              <| fun aggId ->
                  async {
                      let com = Withdraw(Amount = 100)
                      let! r = svc.Withdraw aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (10000m, 10000m, 400m) "聚合数据有误"
                  }
          }
          test "期间生效：转入" {
              setup
              <| fun aggId ->
                  async {
                      let com = TransferIn(Amount = 1000m, OutCode = "123456")
                      let! r = svc.TransferIn aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (10000m, 10000m, 1500m) "聚合数据有误"
                  }
          }
          test "期间生效：转出" {
              setup
              <| fun aggId ->
                  async {
                      let com = TransferOut(Amount = 200m, InCode = "123456")
                      let! r = svc.TransferOut aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (10000m, 10000m, 300m) "聚合数据有误"
                  }
          }
          test "期间生效：变更限额，不变" {
              setup
              <| fun aggId ->
                  async {
                      let com = ChangeLimit(Limit = 10000m)
                      let! r = svc.ChangeLimit aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (10000m, 10000m, 500m) "聚合数据有误"
                  }
          }
          test "期间生效：变更限额，变大" {
              setup
              <| fun aggId ->
                  async {
                      let com = ChangeLimit(Limit = 20000m)
                      let! r = svc.ChangeLimit aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (20000m, 10000m, 500m) "聚合数据有误"
                  }
          }
          test "期间生效：变更限额，变小" {
              setup
              <| fun aggId ->
                  async {
                      let com = ChangeLimit(Limit = 5000m)
                      let! r = svc.ChangeLimit aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (5000m, 5000m, 500m) "聚合数据有误"
                  }
          }
          test "期间生效：变更交易限额" {
              setup
              <| fun aggId ->
                  async {
                      let com = SetTransLimit(TransLimit = 7000m)
                      let! r = svc.SetTransLimit aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId
                      Expect.equal (agg.Limit, agg.TransLimit, agg.Balance) (10000m, 7000m, 500m) "聚合数据有误"
                  }
          } ]
        |> testList "期间生效"
        |> testLabel "Transaction"

    buildTest
    <| fun f ->
        let aggId = Guid.NewGuid()
        let com = OpenPeriod(AccountId = Guid.NewGuid())
        let _ = svc.OpenPeriod aggId (Guid.NewGuid()) com |> Async.RunSynchronously
        let com = SetLimit(Limit = 10000m, TransLimit = 10000m, Balance = 500m)
        let _ = svc.SetLimit aggId (Guid.NewGuid()) com |> Async.RunSynchronously
        f aggId |> Async.RunSynchronously
