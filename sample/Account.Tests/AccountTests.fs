module Account.Tests.Account

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text.Json
open Expecto
open Account.Domain


let client = new HttpClient(BaseAddress = Uri "https://localhost:7180/Account/")
let id1 = Guid.NewGuid()
let id2 = Guid.NewGuid()
let id3 = Guid.NewGuid()

let inline handler aggId (com: 'com) =
    let comType = typeof<'com>.Name
    let content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes com)
    content.Headers.ContentType <- MediaTypeHeaderValue("application/json")
    client.PostAsync($"{comType}/{Guid.NewGuid()}?aggId={aggId}", content).Result


[<Tests>]
let test1 =
    [ testCase "初始化"
      <| fun _ ->
          let com = CreateAccount(Owner = "张三")
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10m)
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "提交设置限额命令"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "提交的限额不变"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.InternalServerError "返回错误"
      testCase "提交的限额小于零"
      <| fun _ ->
          let com = LimitAccount(Limit = -10m)
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.InternalServerError "返回错误" ]
    |> testList "已批准"
    |> testSequenced
    |> testLabel "Account"


[<Tests>]
let test2 =
    [ testCase "初始化"
      <| fun _ ->
          let com = CreateAccount(Owner = "张三")
          let result = handler id2 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = true)
          let result = handler id2 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = false, Limit = 10m)
          let result = handler id2 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "提交设置限额命令"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)
          let result = handler id2 com
          Expect.equal result.StatusCode HttpStatusCode.InternalServerError "返回错误" ]
    |> testList "通过审核，但未批准"
    |> testSequenced
    |> testLabel "Account"


[<Tests>]
let test3 =
    [ testCase "提交设置限额命令"
      <| fun _ ->
          let com = LimitAccount(Limit = 15m)
          Threading.Thread.Sleep 1000
          let result = handler id1 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误" ]
    |> testList "验证缓存刷新后处理"
    |> testSequenced
    |> testLabel "Account"


[<Tests>]
let test4 =
    [ testCase "初始化"
      <| fun _ ->
          let com = CreateAccount(Owner = "张三")
          let result = handler id3 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
          let com = VerifyAccount(VerifiedBy = "王五", Conclusion = false)
          let result = handler id3 com
          Expect.equal result.StatusCode HttpStatusCode.Created "返回错误"
      testCase "提交审批命令"
      <| fun _ ->
          let com = ApproveAccount(ApprovedBy = "赵六", Approved = true, Limit = 10m)
          let result = handler id3 com
          Expect.equal result.StatusCode HttpStatusCode.InternalServerError "返回错误"
      testCase "提交设置限额命令"
      <| fun _ ->
          let com = LimitAccount(Limit = 20m)
          let result = handler id3 com
          Expect.equal result.StatusCode HttpStatusCode.InternalServerError "返回错误" ]
    |> testList "未通过审核"
    |> testSequenced
    |> testLabel "Account"
