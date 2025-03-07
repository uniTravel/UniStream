module Account.Tests.Account

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
    let aggId = Guid.NewGuid()

    [ testAsync "已申请：提交申请" {
          let com = CreateAccount(Owner = "张三")
          let! r = svc.CreateAccount aggId (Guid.NewGuid()) com
          Expect.equal r Success "命令返回结果异常"
          let! agg = svc.Get aggId
          Expect.equal agg.Owner "张三" "聚合数据有误"
      }
      testAsync "未核准：审核" {
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = false)
          let! r = svc.VerifyAccount aggId (Guid.NewGuid()) com
          Expect.equal r Success "命令返回结果异常"
          let! agg = svc.Get aggId

          Expect.equal
              (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion)
              ("张三", "王五", true, false)
              "聚合数据有误"
      } ]
    |> testList "未核准"
    |> testSequenced
    |> testLabel "Account"

[<Tests>]
let test2 =
    let aggId = Guid.NewGuid()

    [ testAsync "已申请：提交申请" {
          let com = CreateAccount(Owner = "张三")
          let! r = svc.CreateAccount aggId (Guid.NewGuid()) com
          Expect.equal r Success "命令返回结果异常"
          let! agg = svc.Get aggId
          Expect.equal agg.Owner "张三" "聚合数据有误"
      }
      testAsync "核准：审核" {
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
          let! r = svc.VerifyAccount aggId (Guid.NewGuid()) com
          Expect.equal r Success "命令返回结果异常"
          let! agg = svc.Get aggId
          Expect.equal (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion) ("张三", "王五", true, true) "聚合数据有误"
      }
      testAsync "未批准：审批" {
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = false, Limit = 10000m)
          let! r = svc.ApproveAccount aggId (Guid.NewGuid()) com
          Expect.equal r Success "命令返回结果异常"
          let! agg = svc.Get aggId

          Expect.equal
              (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion, agg.ApprovedBy, agg.Approved)
              ("张三", "王五", true, true, "赵六", false)
              "聚合数据有误"
      } ]
    |> testList "未批准"
    |> testSequenced
    |> testLabel "Account"

[<Tests>]
let test3 =
    let aggId = Guid.NewGuid()

    [ testAsync "已申请：提交申请" {
          let com = CreateAccount(Owner = "张三")
          let! r = svc.CreateAccount aggId (Guid.NewGuid()) com
          Expect.equal r Success "命令返回结果异常"
          let! agg = svc.Get aggId
          Expect.equal agg.Owner "张三" "聚合数据有误"
      }
      testAsync "核准：审核" {
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
          let! r = svc.VerifyAccount aggId (Guid.NewGuid()) com
          Expect.equal r Success "命令返回结果异常"
          let! agg = svc.Get aggId
          Expect.equal (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion) ("张三", "王五", true, true) "聚合数据有误"
      }
      testAsync "批准：审批" {
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10000m)
          let! r = svc.ApproveAccount aggId (Guid.NewGuid()) com
          Expect.equal r Success "命令返回结果异常"
          let! agg = svc.Get aggId

          Expect.equal
              (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion, agg.ApprovedBy, agg.Approved, agg.Limit)
              ("张三", "王五", true, true, "赵六", true, 10000m)
              "聚合数据有误"
      } ]
    |> testList "批准"
    |> testSequenced
    |> testLabel "Account"

[<Tests>]
let test4 =
    let buildTest setup =
        [ test "批准：修改限额" {
              setup
              <| fun aggId ->
                  async {
                      let com = LimitAccount(Limit = 20000m)
                      let! r = svc.LimitAccount aggId (Guid.NewGuid()) com
                      Expect.equal r Success "命令返回结果异常"
                      let! agg = svc.Get aggId

                      Expect.equal
                          (agg.Owner,
                           agg.VerifiedBy,
                           agg.Verified,
                           agg.VerifyConclusion,
                           agg.ApprovedBy,
                           agg.Approved,
                           agg.Limit)
                          ("张三", "王五", true, true, "赵六", true, 20000m)
                          "聚合数据有误"
                  }
          } ]
        |> testList "批准"
        |> testLabel "Account"

    buildTest
    <| fun f ->
        let aggId = Guid.NewGuid()
        let com = CreateAccount(Owner = "张三")
        let _ = svc.CreateAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously
        let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
        let _ = svc.VerifyAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously
        let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10000m)
        let _ = svc.ApproveAccount aggId (Guid.NewGuid()) com |> Async.RunSynchronously
        f aggId |> Async.RunSynchronously
