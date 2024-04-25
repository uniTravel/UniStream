module Account.Sender.Tests.Sender

open System
open Microsoft.Extensions.DependencyInjection
open Expecto
open UniStream.Domain
open Account.Domain


let client = host.Services.GetRequiredService<IClient>()
let agent = Channel.init client
let mutable id = Guid.Empty
let acid = Guid.NewGuid()


[<Tests>]
let test1 =
    [ testCase "初始化1"
      <| fun _ ->
          let com =
              InitPeriod(AccountId = acid, Period = $"{DateTime.Today:yyyyMM}", Limit = 1000m)

          id <- Guid.NewGuid()
          Channel.send agent id com |> Async.StartImmediate
      testCase "存款1"
      <| fun _ ->
          let com = Deposit(Amount = 5m)
          Channel.send agent id com |> Async.StartImmediate
      testCase "取款1"
      <| fun _ ->
          let com = Withdraw(Amount = 1m)
          Channel.send agent id com |> Async.StartImmediate
      testCase "转入1"
      <| fun _ ->
          let com = TransferIn(Amount = 100m)
          Channel.send agent id com |> Async.StartImmediate
      testCase "转出1"
      <| fun _ ->
          let com = TransferOut(Amount = 10m)
          Channel.send agent id com |> Async.StartImmediate
      testCase "设置交易限额"
      <| fun _ ->
          let com = SetTransLimit(TransLimit = 500m)
          Channel.send agent id com |> Async.StartImmediate
      testCase "变更的限额小于交易限额"
      <| fun _ ->
          let com = ChangeLimit(Limit = 300m)
          Channel.send agent id com |> Async.StartImmediate
      testCase "初始化2"
      <| fun _ ->
          let next = DateTime.Today.AddMonths(1)
          let com = OpenPeriod(AccountId = acid, Period = $"{next:yyyyMM}")
          id <- Guid.NewGuid()
          Channel.send agent id com |> Async.StartImmediate
      testCase "过账限额、余额以生效交易期间"
      <| fun _ ->
          let com = SetLimit(Limit = 1000m, TransLimit = 1000m, Balance = 94m)
          Channel.send agent id com |> Async.StartImmediate
      testCase "存款2"
      <| fun _ ->
          let com = Deposit(Amount = 5m)
          Channel.send agent id com |> Async.StartImmediate
      testCase "取款2"
      <| fun _ ->
          let com = Withdraw(Amount = 1m)
          Channel.send agent id com |> Async.StartImmediate
      testCase "转入2"
      <| fun _ ->
          let com = TransferIn(Amount = 100m)
          Channel.send agent id com |> Async.StartImmediate
      testCase "转出2"
      <| fun _ ->
          let com = TransferOut(Amount = 10m)
          Channel.send agent id com |> Async.StartImmediate
      testCase "" <| fun _ -> Threading.Thread.Sleep 5000 ]
    |> testList "初始创建的交易期间"
    |> testSequenced
    |> testLabel "Sender"
