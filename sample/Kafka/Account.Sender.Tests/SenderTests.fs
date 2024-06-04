module Account.Sender.Tests.Sender

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text.Json
open Expecto
open Account.Domain


let client = new HttpClient(BaseAddress = Uri "https://localhost:7280/Transaction/")
let id1 = Guid.NewGuid()
let id2 = Guid.NewGuid()
let acid = Guid.NewGuid()

let inline handler aggId (com: 'com) =
    let comType = typeof<'com>.Name
    let content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes com)
    content.Headers.ContentType <- MediaTypeHeaderValue("application/json")
    client.PostAsync($"{comType}/{Guid.NewGuid()}?aggId={aggId}", content).Result


[<Tests>]
let test1 =
    [ testCase "初始化1"
      <| fun _ ->
          let com =
              InitPeriod(AccountId = acid, Period = $"{DateTime.Today:yyyyMM}", Limit = 1000m)

          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "存款1"
      <| fun _ ->
          let com = Deposit(Amount = 5m)
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "取款1"
      <| fun _ ->
          let com = Withdraw(Amount = 1m)
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "转入1"
      <| fun _ ->
          let com = TransferIn(Amount = 100m)
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "转出1"
      <| fun _ ->
          let com = TransferOut(Amount = 10m)
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "设置交易限额"
      <| fun _ ->
          let com = SetTransLimit(TransLimit = 500m)
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "变更的限额小于交易限额"
      <| fun _ ->
          let com = ChangeLimit(Limit = 300m)
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "初始化2"
      <| fun _ ->
          let next = DateTime.Today.AddMonths(1)
          let com = OpenPeriod(AccountId = acid, Period = $"{next:yyyyMM}")
          let result = handler id2 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "过账限额、余额以生效交易期间"
      <| fun _ ->
          let com = SetLimit(Limit = 1000m, TransLimit = 1000m, Balance = 94m)
          let result = handler id2 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "存款2"
      <| fun _ ->
          let com = Deposit(Amount = 5m)
          let result = handler id2 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "取款2"
      <| fun _ ->
          let com = Withdraw(Amount = 1m)
          let result = handler id2 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "转入2"
      <| fun _ ->
          let com = TransferIn(Amount = 100m)
          let result = handler id2 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "转出2"
      <| fun _ ->
          let com = TransferOut(Amount = 10m)
          let result = handler id2 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误" ]
    |> testList "初始创建的交易期间"
    |> testSequenced
    |> testLabel "Sender"
