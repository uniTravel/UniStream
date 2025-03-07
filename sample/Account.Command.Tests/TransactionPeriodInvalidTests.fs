module Account.Tests.Transaction.PeriodInvalid

open System
open System.Collections.Generic
open Expecto
open FsCheck
open UniStream.Domain
open Account.Domain
open Account.TestFixture


[<Tests>]
let test1 =
    let buildTest setup =
        [ test "Deposit命令" {
              setup
              <| fun agent aggId ->
                  async {
                      btw 1 10000000 <| fun a -> Deposit(Amount = a)
                      |> Gen.sample 0 200
                      |> List.iter (fun com ->
                          let f = call <| deposit agent aggId (Guid.NewGuid()) com
                          Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> Async.RunSynchronously)
                  }
          }
          test "Withdraw命令" {
              setup
              <| fun agent aggId ->
                  async {
                      btw 1 10000000 <| fun a -> Withdraw(Amount = a)
                      |> Gen.sample 0 200
                      |> List.iter (fun com ->
                          let f = call <| withdraw agent aggId (Guid.NewGuid()) com
                          Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> Async.RunSynchronously)
                  }
          }
          test "TransferIn命令" {
              setup
              <| fun agent aggId ->
                  async {
                      btw 1 10000000 <| fun a -> TransferIn(Amount = a, OutCode = "123456")
                      |> Gen.sample 0 200
                      |> List.iter (fun com ->
                          let f = call <| transferIn agent aggId (Guid.NewGuid()) com
                          Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> Async.RunSynchronously)
                  }
          }
          test "TransferOut命令" {
              setup
              <| fun agent aggId ->
                  async {
                      btw 1 10000000 <| fun a -> TransferOut(Amount = a, InCode = "123456")
                      |> Gen.sample 0 200
                      |> List.iter (fun com ->
                          let f = call <| transferOut agent aggId (Guid.NewGuid()) com
                          Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> Async.RunSynchronously)
                  }
          }
          test "ChangeLimit命令" {
              setup
              <| fun agent aggId ->
                  async {
                      btw 10000 10000000 <| fun l -> ChangeLimit(Limit = l)
                      |> Gen.sample 0 200
                      |> List.iter (fun com ->
                          let f = call <| changeLimit agent aggId (Guid.NewGuid()) com
                          Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> Async.RunSynchronously)
                  }
          }
          test "SetTransLimit命令" {
              setup
              <| fun agent aggId ->
                  async {
                      btw 10000 10000000 <| fun l -> SetTransLimit(TransLimit = l)
                      |> Gen.sample 0 200
                      |> List.iter (fun com ->
                          let f = call <| setTransLimit agent aggId (Guid.NewGuid()) com
                          Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> Async.RunSynchronously)
                  }
          } ]

    buildTest
    <| fun f ->
        let repo = Dictionary<string, (uint64 * string * ReadOnlyMemory<byte>) list> 10000
        let aggType = typeof<Transaction>.FullName

        let stream =
            { new IStream<Transaction> with
                member _.Writer = writer repo aggType
                member _.Reader = reader repo aggType
                member _.Restore = fun _ _ -> [] }

        let opt = AggregateOptions(Capacity = 10000)
        let agent = Aggregator.init Transaction stream opt
        Aggregator.register agent <| Replay<Transaction, PeriodInited>()
        Aggregator.register agent <| Replay<Transaction, PeriodOpened>()
        Aggregator.register agent <| Replay<Transaction, LimitSetted>()
        Aggregator.register agent <| Replay<Transaction, LimitChanged>()
        Aggregator.register agent <| Replay<Transaction, TransLimitSetted>()
        Aggregator.register agent <| Replay<Transaction, DepositFinished>()
        Aggregator.register agent <| Replay<Transaction, WithdrawFinished>()
        Aggregator.register agent <| Replay<Transaction, TransferInFinished>()
        Aggregator.register agent <| Replay<Transaction, TransferOutFinished>()

        let aggId = Guid.NewGuid()
        let com = OpenPeriod(AccountId = Guid.NewGuid())
        let _ = openPeriod agent aggId (Guid.NewGuid()) com |> Async.RunSynchronously

        try
            f agent aggId |> Async.RunSynchronously
        finally
            repo.Clear()
            agent.Dispose()
    |> testList "Transaction.期间未生效"
    |> testLabel "Aggregate"
