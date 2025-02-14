module Account.Tests.Account.Init

open System
open Microsoft.Extensions.DependencyInjection
open Expecto
open UniStream.Domain
open Account.Domain
open Account.Application
open Account.TestFixture


let svc = provider.GetRequiredService<AccountService>()

[<Tests>]
let test1 =
    let buildTest setup =
        [ test "ApproveAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10m)
                      let f = fun _ -> svc.ApproveAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          }
          test "LimitAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = LimitAccount(Limit = 20)
                      let f = fun _ -> svc.LimitAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          }
          test "CreateAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = CreateAccount(Owner = "张三")
                      let f = fun _ -> svc.CreateAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<WriteException> f "错误类型有误" |> ignore
                  }
          }
          test "VerifyAccount命令，核准" {
              setup
              <| fun aggId ->
                  async {
                      let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
                      let! _ = svc.VerifyAccount aggId (Guid.NewGuid()) com
                      let! agg = svc.Get aggId

                      Expect.equal
                          (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion)
                          ("张三", "王五", true, true)
                          "聚合数据有误"
                  }
          }
          test "VerifyAccount命令，不核准" {
              setup
              <| fun aggId ->
                  async {
                      let com = VerifyAccount(VerifiedBy = "王五", Conclusion = false)
                      let! _ = svc.VerifyAccount aggId (Guid.NewGuid()) com
                      let! agg = svc.Get aggId

                      Expect.equal
                          (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion)
                          ("张三", "王五", true, false)
                          "聚合数据有误"
                  }
          } ]

    buildTest
    <| fun f ->
        let aggId = Guid.NewGuid()
        let com = CreateAccount(Owner = "张三")
        svc.CreateAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore
        f aggId |> Async.RunSynchronously
    |> testList "初始"
    |> testLabel "Account"

[<Tests>]
let test2 =
    let buildTest setup =
        [ test "VerifyAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
                      let f = fun _ -> svc.VerifyAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          }
          test "ApproveAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10m)
                      let f = fun _ -> svc.ApproveAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          }
          test "LimitAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = LimitAccount(Limit = 20)
                      let f = fun _ -> svc.LimitAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          } ]

    buildTest
    <| fun f ->
        let aggId = Guid.NewGuid()
        let com = CreateAccount(Owner = "张三")
        svc.CreateAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore
        let com = VerifyAccount(VerifiedBy = "王五", Conclusion = false)
        svc.VerifyAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore
        f aggId |> Async.RunSynchronously
    |> testList "未核准"
    |> testLabel "Account"

[<Tests>]
let test3 =
    let buildTest setup =
        [ test "VerifyAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
                      let f = fun _ -> svc.VerifyAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          }
          test "LimitAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = LimitAccount(Limit = 20)
                      let f = fun _ -> svc.LimitAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          }
          test "ApproveAccount命令，不批准" {
              setup
              <| fun aggId ->
                  async {
                      let com = ApproveAccount(ApprovedBy = "赵六", Approved = false, Limit = 10m)
                      let! _ = svc.ApproveAccount aggId (Guid.NewGuid()) com
                      let! agg = svc.Get aggId

                      Expect.equal
                          (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion, agg.ApprovedBy, agg.Approved)
                          ("张三", "王五", true, true, "赵六", false)
                          "聚合数据有误"
                  }
          }
          test "ApproveAccount命令，批准" {
              setup
              <| fun aggId ->
                  async {
                      let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10m)
                      let! _ = svc.ApproveAccount aggId (Guid.NewGuid()) com
                      let! agg = svc.Get aggId

                      Expect.equal
                          (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion, agg.ApprovedBy, agg.Approved)
                          ("张三", "王五", true, true, "赵六", true)
                          "聚合数据有误"
                  }
          }
          test "ApproveAccount命令，Limit为0" {
              setup
              <| fun aggId ->
                  async {
                      let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 0m)
                      let f = fun _ -> svc.ApproveAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          }
          test "ApproveAccount命令，Limit小于0" {
              setup
              <| fun aggId ->
                  async {
                      let com = ApproveAccount(ApprovedBy = "赵六", Approved = false, Limit = -10m)
                      let f = fun _ -> svc.ApproveAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          } ]

    buildTest
    <| fun f ->
        let aggId = Guid.NewGuid()
        let com = CreateAccount(Owner = "张三")
        svc.CreateAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore
        let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
        svc.VerifyAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore
        f aggId |> Async.RunSynchronously
    |> testList "核准"
    |> testLabel "Account"

[<Tests>]
let test4 =
    let buildTest setup =
        [ test "LimitAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = LimitAccount(Limit = 20)
                      let f = fun _ -> svc.LimitAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          }
          test "CreateAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = CreateAccount(Owner = "张三")
                      let f = fun _ -> svc.CreateAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<WriteException> f "错误类型有误" |> ignore
                  }
          }
          test "VerifyAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
                      let f = fun _ -> svc.VerifyAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          } ]

    buildTest
    <| fun f ->
        let aggId = Guid.NewGuid()
        let com = CreateAccount(Owner = "张三")
        svc.CreateAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore
        let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
        svc.VerifyAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore
        let com = ApproveAccount(ApprovedBy = "赵六", Approved = false, Limit = 10m)

        svc.ApproveAccount aggId (Guid.NewGuid()) com
        |> Async.RunSynchronously
        |> ignore

        f aggId |> Async.RunSynchronously
    |> testList "未批准"
    |> testLabel "Account"

[<Tests>]
let test5 =
    let buildTest setup =
        [ test "CreateAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = CreateAccount(Owner = "张三")
                      let f = fun _ -> svc.CreateAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<WriteException> f "错误类型有误" |> ignore
                  }
          }
          test "VerifyAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
                      let f = fun _ -> svc.VerifyAccount aggId (Guid.NewGuid()) com |> Async.Ignore
                      Expect.throwsAsyncT<ValidateException> f "错误类型有误" |> ignore
                  }
          }
          test "LimitAccount命令" {
              setup
              <| fun aggId ->
                  async {
                      let com = LimitAccount(Limit = 20)
                      let! _ = svc.LimitAccount aggId (Guid.NewGuid()) com
                      let! agg = svc.Get aggId

                      Expect.equal
                          (agg.Owner,
                           agg.VerifiedBy,
                           agg.Verified,
                           agg.VerifyConclusion,
                           agg.ApprovedBy,
                           agg.Approved,
                           agg.Limit)
                          ("张三", "王五", true, true, "赵六", true, 20m)
                          "聚合数据有误"
                  }
          } ]

    buildTest
    <| fun f ->
        let aggId = Guid.NewGuid()
        let com = CreateAccount(Owner = "张三")
        svc.CreateAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore
        let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
        svc.VerifyAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore
        let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10m)

        svc.ApproveAccount aggId (Guid.NewGuid()) com
        |> Async.RunSynchronously
        |> ignore

        f aggId |> Async.RunSynchronously
    |> testList "批准"
    |> testLabel "Account"
