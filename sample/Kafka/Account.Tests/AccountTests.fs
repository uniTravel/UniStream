module Account.Tests.Account

open System
open Microsoft.Extensions.DependencyInjection
open Expecto
open Account.Domain
open Account.Application


let svc = host.Services.GetRequiredService<AccountService>()
let id1 = Guid.NewGuid()
let id3 = Guid.NewGuid()
let id2 = Guid.NewGuid()


[<Tests>]
let test1 =
    [ testCase "初始化"
      <| fun _ ->
          let com = CreateAccount(Owner = "张三")
          svc.CreateAccount id1 (Guid.NewGuid()) com |> Async.RunSynchronously
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
          svc.VerifyAccount id1 (Guid.NewGuid()) com |> Async.RunSynchronously
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10m)
          svc.ApproveAccount id1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "提交设置限额命令"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)
          svc.LimitAccount id1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "提交的限额不变"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)

          let f =
              fun _ -> svc.LimitAccount id1 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "提交的限额小于零"
      <| fun _ ->
          let com = LimitAccount(Limit = -10m)

          let f =
              fun _ -> svc.LimitAccount id1 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<ValidateError> f "异常类型有误" ]
    |> testList "已批准"
    |> testSequenced
    |> testLabel "Account"


[<Tests>]
let test2 =
    [ testCase "初始化"
      <| fun _ ->
          let com = CreateAccount(Owner = "张三")
          svc.CreateAccount id2 (Guid.NewGuid()) com |> Async.RunSynchronously
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
          svc.VerifyAccount id2 (Guid.NewGuid()) com |> Async.RunSynchronously
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = false, Limit = 10m)
          svc.ApproveAccount id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "提交设置限额命令"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)

          let f =
              fun _ -> svc.LimitAccount id2 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<ValidateError> f "异常类型有误" ]
    |> testList "通过审核，但未批准"
    |> testSequenced
    |> testLabel "Account"


[<Tests>]
let test3 =
    [ testCase "提交设置限额命令"
      <| fun _ ->
          let com = LimitAccount(Limit = 15m)
          Threading.Thread.Sleep 1000
          svc.LimitAccount id1 (Guid.NewGuid()) com |> Async.RunSynchronously ]
    |> testList "验证缓存刷新后处理"
    |> testSequenced
    |> testLabel "Account"


[<Tests>]
let test4 =
    [ testCase "初始化"
      <| fun _ ->
          let com = CreateAccount(Owner = "张三")
          svc.CreateAccount id3 (Guid.NewGuid()) com |> Async.RunSynchronously
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = false)
          svc.VerifyAccount id3 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "提交审批命令"
      <| fun _ ->
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10m)

          let f =
              fun _ -> svc.ApproveAccount id3 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "提交设置限额命令"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)

          let f =
              fun _ -> svc.LimitAccount id3 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<ValidateError> f "异常类型有误" ]
    |> testList "未通过审核"
    |> testSequenced
    |> testLabel "Account"
