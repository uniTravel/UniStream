namespace Account.Api.Controllers

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Account.Application
open Account.Domain


[<ApiController>]
[<Route("[controller]/[action]")>]
type AccountController(logger: ILogger<AccountController>, svc: AccountService) as me =
    inherit ControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.CreateAccount(aggId: Guid, com: CreateAccount) =
        task {
            let! result = svc.CreateAccount aggId com
            return me.CreatedAtAction(nameof (me.CreateAccount), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.VerifyAccount(aggId: Guid, com: VerifyAccount) =
        task {
            let! result = svc.VerifyAccount aggId com
            return me.CreatedAtAction(nameof (me.VerifyAccount), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.ApproveAccount(aggId: Guid, com: ApproveAccount) =
        task {
            let! result = svc.ApproveAccount aggId com
            return me.CreatedAtAction(nameof (me.ApproveAccount), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.LimitAccount(aggId: Guid, com: LimitAccount) =
        task {
            let! result = svc.LimitAccount aggId com
            return me.CreatedAtAction(nameof (me.LimitAccount), result)
        }
