module Account.Tests.Account.Command

open Expecto
open FsCheck
open Account.Domain
open Account.TestFixture


[<Tests>]
let test1 =
    [ test "缺省的命令" {
          let com = CreateAccount()
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.owner "验证错误提示不对"
      } ]
    |> testList "Account.CreateAccount"
    |> testLabel "Command"

[<Tests>]
let test2 =
    [ test "缺省的命令" {
          let com = VerifyAccount()
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.verifiedBy "验证错误提示不对"
      } ]
    |> testList "Account.VerifyAccount"
    |> testLabel "Command"

[<Tests>]
let test3 =
    [ test "缺省的命令" {
          let com = ApproveAccount()
          let r = validateModel com
          Expect.hasLength r 2 "验证错误数量不对"
          Expect.contains r ValidateError.approvedBy "验证错误提示不对"
          Expect.contains r ValidateError.limit "验证错误提示不对"
      }
      test "批准的账户，限额为零" {
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 0m)
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.limit "验证错误提示不对"
          let r = validate com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.approvedLimit "验证错误提示不对"
      }
      test "批准的账户，限额为负" {
          minus <| fun l -> ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对"
              let r = validate com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.approvedLimit "验证错误提示不对")
      }
      test "金额在范围内，但小数位数为3" {
          scale 3 100 100000
          <| fun l -> ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，金额为负" {
          minus <| fun l -> ApproveAccount(ApprovedBy = "赵六", Approved = false, Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，金额在0到99.99之间" {
          btw 0 9999
          <| fun l -> ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对")
      }
      test "小数位数不超过2，金额大于100000" {
          gte 100000001
          <| fun l -> ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对")
      } ]
    |> testList "Account.ApproveAccount"
    |> testLabel "Command"

[<Tests>]
let test4 =
    [ test "缺省的命令" {
          let com = LimitAccount()
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.limit "验证错误提示不对"
      }
      test "金额在范围内，但小数位数为3" {
          scale 3 100 100000 <| fun l -> LimitAccount(Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，金额为负" {
          minus <| fun l -> LimitAccount(Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，金额在0到99.99之间" {
          btw 0 9999 <| fun l -> LimitAccount(Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对")
      }
      test "小数位数不超过2，金额大于100000" {
          gte 100000001 <| fun l -> LimitAccount(Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对")
      } ]
    |> testList "Account.LimitAccount"
    |> testLabel "Command"
