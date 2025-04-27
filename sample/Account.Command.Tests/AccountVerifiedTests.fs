module Account.Tests.Account.Verified

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
        [ test "VerifyAccount命令" {
              setup
              <| fun agent aggId ->
                  async {
                      Gen.map
                          (fun c -> VerifyAccount(VerifiedBy = "王五", Conclusion = c))
                          (Gen.oneof [ Gen.constant true; Gen.constant false ])
                      |> Gen.sample 0 200
                      |> List.iter (fun com ->
                          let f = call <| verifyAccount agent aggId (Guid.NewGuid()) com
                          Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> Async.RunSynchronously)
                  }
          }
          test "LimitAccount命令" {
              setup
              <| fun agent aggId ->
                  async {
                      btw 10000 10000000 <| fun l -> LimitAccount(Limit = l)
                      |> Gen.sample 0 200
                      |> List.iter (fun com ->
                          let f = call <| limitAccount agent aggId (Guid.NewGuid()) com
                          Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> Async.RunSynchronously)
                  }
          } ]

    buildTest
    <| fun f ->
        let repo = Dictionary<string, (uint64 * string * ReadOnlyMemory<byte>) list> 10000
        let aggType = typeof<Account>.FullName

        let stream =
            { new IStream<Account> with
                member _.Writer = writer repo aggType
                member _.Reader = reader repo aggType
                member _.Restore = fun _ _ -> [] }

        let opt = AggregateOptions(Capacity = 10000)
        let agent = Aggregator.init Account cts.Token stream opt
        Aggregator.register agent <| Replay<Account, AccountCreated>()
        Aggregator.register agent <| Replay<Account, AccountVerified>()
        Aggregator.register agent <| Replay<Account, AccountApproved>()
        Aggregator.register agent <| Replay<Account, AccountLimited>()

        let aggId = Guid.NewGuid()
        let com = CreateAccount(Owner = "张三")
        let _ = createAccount agent aggId (Guid.NewGuid()) com |> Async.RunSynchronously
        let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
        let _ = verifyAccount agent aggId (Guid.NewGuid()) com |> Async.RunSynchronously

        try
            f agent aggId |> Async.RunSynchronously
        finally
            repo.Clear()
            agent.Dispose()
    |> testList "Account.核准"
    |> testLabel "Aggregate"
