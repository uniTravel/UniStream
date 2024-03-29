module Account.Tests.Account

open System
open Expecto
open Account.Domain
open Account.Application


let svc = AccountService(es.Write, es.Read, 10000, 0.2)
let mutable id = Guid.Empty


[<Tests>]
let test1 =
    [ testCase "初始化"
      <| fun _ ->
          let com = CreateAccount(Owner = "张三")
          let agg = svc.CreateAccount (Guid.NewGuid()) com |> Async.RunSynchronously
          id <- agg.Id
          Expect.equal agg.Owner "张三" "聚合值有误"
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = false)
          let agg = svc.VerifyAccount id com |> Async.RunSynchronously
          Expect.equal (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion) ("张三", "王五", true, false) "聚合值有误"
      testCase "提交审批命令"
      <| fun _ ->
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10m)
          let f = fun _ -> svc.ApproveAccount id com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "提交设置限额命令"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)
          let f = fun _ -> svc.LimitAccount id com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "异常类型有误" ]
    |> testList "未通过审核"
    |> testSequenced
    |> testLabel "Account"


[<Tests>]
let test2 =
    [ testCase "初始化"
      <| fun _ ->
          let com = CreateAccount(Owner = "张三")
          let agg = svc.CreateAccount (Guid.NewGuid()) com |> Async.RunSynchronously
          id <- agg.Id
          Expect.equal agg.Owner "张三" "聚合值有误"
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
          let agg = svc.VerifyAccount id com |> Async.RunSynchronously
          Expect.equal (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion) ("张三", "王五", true, true) "聚合值有误"
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = false, Limit = 10m)
          let agg = svc.ApproveAccount id com |> Async.RunSynchronously

          Expect.equal
              (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion, agg.ApprovedBy, agg.Approved, agg.Limit)
              ("张三", "王五", true, true, "赵六", false, 0m)
              "聚合值有误"
      testCase "提交设置限额命令"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)
          let f = fun _ -> svc.LimitAccount id com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "异常类型有误" ]
    |> testList "通过审核，但未批准"
    |> testSequenced
    |> testLabel "Account"


[<Tests>]
let test3 =
    [ testCase "初始化"
      <| fun _ ->
          let com = CreateAccount(Owner = "张三")
          let agg = svc.CreateAccount (Guid.NewGuid()) com |> Async.RunSynchronously
          id <- agg.Id
          Expect.equal agg.Owner "张三" "聚合值有误"
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
          let agg = svc.VerifyAccount id com |> Async.RunSynchronously
          Expect.equal (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion) ("张三", "王五", true, true) "聚合值有误"
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10m)
          let agg = svc.ApproveAccount id com |> Async.RunSynchronously

          Expect.equal
              (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion, agg.ApprovedBy, agg.Approved, agg.Limit)
              ("张三", "王五", true, true, "赵六", true, 10m)
              "聚合值有误"
      testCase "提交设置限额命令"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)
          let agg = svc.LimitAccount id com |> Async.RunSynchronously

          Expect.equal
              (agg.Owner, agg.VerifiedBy, agg.Verified, agg.VerifyConclusion, agg.ApprovedBy, agg.Approved, agg.Limit)
              ("张三", "王五", true, true, "赵六", true, 20m)
              "聚合值有误"
      testCase "提交的限额不变"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)
          let f = fun _ -> svc.LimitAccount id com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "提交的限额小于零"
      <| fun _ ->
          let com = LimitAccount(Limit = -10m)
          let f = fun _ -> svc.LimitAccount id com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "异常类型有误" ]
    |> testList "已批准"
    |> testSequenced
    |> testLabel "Account"
