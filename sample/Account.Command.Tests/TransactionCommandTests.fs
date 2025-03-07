module Account.Tests.Transaction.Command

open System
open Expecto
open FsCheck
open Account.Domain
open Account.TestFixture


[<Tests>]
let test1 =
    [ test "缺省的命令" {
          let com = InitPeriod()
          let r = validateModel com
          Expect.hasLength r 2 "验证错误数量不对"
          Expect.contains r ValidateError.guid "验证错误提示不对"
          Expect.contains r ValidateError.limit "验证错误提示不对"
      }
      test "金额在范围内，但小数位数为3" {
          scale 3 100 100000 <| fun l -> InitPeriod(AccountId = Guid.NewGuid(), Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，金额为负" {
          minus <| fun l -> InitPeriod(AccountId = Guid.NewGuid(), Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，金额在0到99.99之间" {
          btw 0 9999 <| fun l -> InitPeriod(AccountId = Guid.NewGuid(), Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对")
      }
      test "小数位数不超过2，金额大于100000" {
          gte 100000001 <| fun l -> InitPeriod(AccountId = Guid.NewGuid(), Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对")
      } ]
    |> testList "Transaction.InitPeriod"
    |> testLabel "Command"

[<Tests>]
let test2 =
    [ test "缺省的命令" {
          let com = OpenPeriod()
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.guid "验证错误提示不对"
      } ]
    |> testList "Transaction.OpenPeriod"
    |> testLabel "Command"

[<Tests>]
let test3 =
    [ test "缺省的命令" {
          let com = SetLimit()
          let r = validateModel com
          Expect.hasLength r 2 "验证错误数量不对"
          Expect.contains r ValidateError.limit "验证错误提示不对"
          Expect.contains r ValidateError.transLimit "验证错误提示不对"
      }
      test "限额在范围内，但小数位数为3" {
          scale 3 100 100000 <| fun l -> SetLimit(Limit = l, TransLimit = 100m)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，限额为负" {
          minus <| fun l -> SetLimit(Limit = l, TransLimit = 100m)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，限额在0到99.99之间" {
          btw 0 9999 <| fun l -> SetLimit(Limit = l, TransLimit = 100m)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对")
      }
      test "小数位数不超过2，限额大于100000" {
          gte 100000001 <| fun l -> SetLimit(Limit = l, TransLimit = 100m)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对")
      }
      test "交易限额在范围内，但小数位数为3" {
          scale 3 100 100000 <| fun l -> SetLimit(Limit = 100000m, TransLimit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易限额为负" {
          minus <| fun l -> SetLimit(Limit = 100000m, TransLimit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.transLimit "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易限额在0到99.99之间" {
          btw 0 9999 <| fun l -> SetLimit(Limit = 100000m, TransLimit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.transLimit "验证错误提示不对")
      }
      test "小数位数不超过2，交易限额大于100000" {
          gte 100000001 <| fun l -> SetLimit(Limit = 100000m, TransLimit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.transLimit "验证错误提示不对")
      }
      test "限额在范围内, 交易限额超过控制限额" {
          let com = SetLimit(Limit = 1000m, TransLimit = 10000m, Balance = 100m)
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.limitTranslimit "验证错误提示不对"
          let r = validate com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.limitTranslimit "验证错误提示不对"
      }
      test "余额在范围内，但小数位数为3" {
          scale 3 100 100000
          <| fun l -> SetLimit(Limit = 10000m, TransLimit = 10000m, Balance = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，余额为负" {
          minus <| fun l -> SetLimit(Limit = 10000m, TransLimit = 10000m, Balance = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.balance "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      } ]
    |> testList "Transaction.SetLimit"
    |> testLabel "Command"

[<Tests>]
let test4 =
    [ test "缺省的命令" {
          let com = ChangeLimit()
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.limit "验证错误提示不对"
      }
      test "限额在范围内，但小数位数为3" {
          scale 3 100 100000 <| fun l -> ChangeLimit(Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，限额为负" {
          minus <| fun l -> ChangeLimit(Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，限额在0到99.99之间" {
          btw 0 9999 <| fun l -> ChangeLimit(Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对")
      }
      test "小数位数不超过2，限额大于100000" {
          gte 100000001 <| fun l -> ChangeLimit(Limit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.limit "验证错误提示不对")
      } ]
    |> testList "Transaction.ChangeLimit"
    |> testLabel "Command"

[<Tests>]
let test5 =
    [ test "缺省的命令" {
          let com = SetTransLimit()
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.transLimit "验证错误提示不对"
      }
      test "交易限额在范围内，但小数位数为3" {
          scale 3 100 100000 <| fun l -> SetTransLimit(TransLimit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易限额为负" {
          minus <| fun l -> SetTransLimit(TransLimit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.transLimit "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易限额在0到99.99之间" {
          btw 0 9999 <| fun l -> SetTransLimit(TransLimit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.transLimit "验证错误提示不对")
      }
      test "小数位数不超过2，交易限额大于100000" {
          gte 100000001 <| fun l -> SetTransLimit(TransLimit = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.transLimit "验证错误提示不对")
      } ]
    |> testList "Transaction.SetTransLimit"
    |> testLabel "Command"

[<Tests>]
let test6 =
    [ test "缺省的命令" {
          let com = Deposit()
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.amount "验证错误提示不对"
      }
      test "交易金额在范围内，但小数位数为3" {
          scale 3 100 100000 <| fun l -> Deposit(Amount = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易金额为负" {
          minus <| fun l -> Deposit(Amount = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.amount "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易金额为零" {
          let com = Deposit(Amount = 0m)
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.amount "验证错误提示不对"
      } ]
    |> testList "Transaction.Deposit"
    |> testLabel "Command"

[<Tests>]
let test7 =
    [ test "缺省的命令" {
          let com = Withdraw()
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.amount "验证错误提示不对"
      }
      test "交易金额在范围内，但小数位数为3" {
          scale 3 100 100000 <| fun l -> Withdraw(Amount = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易金额为负" {
          minus <| fun l -> Withdraw(Amount = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.amount "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易金额为零" {
          let com = Withdraw(Amount = 0m)
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.amount "验证错误提示不对"
      } ]
    |> testList "Transaction.Withdraw"
    |> testLabel "Command"

[<Tests>]
let test8 =
    [ test "缺省的命令" {
          let com = TransferOut()
          let r = validateModel com
          Expect.hasLength r 2 "验证错误数量不对"
          Expect.contains r ValidateError.accountCode "验证错误提示不对"
          Expect.contains r ValidateError.amount "验证错误提示不对"
      }
      test "交易金额在范围内，但小数位数为3" {
          scale 3 100 100000 <| fun l -> TransferOut(InCode = "123456", Amount = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易金额为负" {
          minus <| fun l -> TransferOut(InCode = "123456", Amount = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.amount "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易金额为零" {
          let com = TransferOut(InCode = "123456", Amount = 0m)
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.amount "验证错误提示不对"
      } ]
    |> testList "Transaction.TransferOut"
    |> testLabel "Command"

[<Tests>]
let test9 =
    [ test "缺省的命令" {
          let com = TransferIn()
          let r = validateModel com
          Expect.hasLength r 2 "验证错误数量不对"
          Expect.contains r ValidateError.accountCode "验证错误提示不对"
          Expect.contains r ValidateError.amount "验证错误提示不对"
      }
      test "交易金额在范围内，但小数位数为3" {
          scale 3 100 100000 <| fun l -> TransferIn(OutCode = "123456", Amount = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 1 "验证错误数量不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易金额为负" {
          minus <| fun l -> TransferIn(OutCode = "123456", Amount = l)
          |> Gen.sample 0 200
          |> List.iter (fun com ->
              let r = validateModel com
              Expect.hasLength r 2 "验证错误数量不对"
              Expect.contains r ValidateError.amount "验证错误提示不对"
              Expect.contains r ValidateError.money "验证错误提示不对")
      }
      test "小数位数不超过2，交易金额为零" {
          let com = TransferIn(OutCode = "123456", Amount = 0m)
          let r = validateModel com
          Expect.hasLength r 1 "验证错误数量不对"
          Expect.contains r ValidateError.amount "验证错误提示不对"
      } ]
    |> testList "Transaction.TransferIn"
    |> testLabel "Command"
