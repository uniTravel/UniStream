namespace Account.Api.Controllers

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Application
open Account.Domain


[<ApiController>]
[<Route("[controller]/[action]/{comId}")>]
type AccountController(logger: ILogger<AccountController>, svc: AccountService) as me =
    inherit ControllerBase()

    let convert result =
        match result with
        | Success -> ()
        | Duplicate -> ()
        | Fail ex -> raise ex

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.CreateAccount(aggId: Guid, comId: Guid, com: CreateAccount) =
        task {
            let! result = svc.CreateAccount aggId comId com
            return me.CreatedAtAction(nameof me.CreateAccount, convert result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.VerifyAccount(aggId: Guid, comId: Guid, com: VerifyAccount) =
        task {
            let! result = svc.VerifyAccount aggId comId com
            return me.CreatedAtAction(nameof me.VerifyAccount, convert result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.ApproveAccount(aggId: Guid, comId: Guid, com: ApproveAccount) =
        task {
            let! result = svc.ApproveAccount aggId comId com
            return me.CreatedAtAction(nameof me.ApproveAccount, convert result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.LimitAccount(aggId: Guid, comId: Guid, com: LimitAccount) =
        task {
            let! result = svc.LimitAccount aggId comId com
            return me.CreatedAtAction(nameof me.LimitAccount, convert result)
        }
