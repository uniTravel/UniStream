namespace Account.Sender.Controller

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain


[<ApiController>]
[<Route("[controller]/[action]/{comId}")>]
type AccountController(logger: ILogger<AccountController>, sender: ISender<Account>) as me =
    inherit ControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.CreateAccount(aggId: Guid, comId: Guid, com: CreateAccount) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.CreateAccount, result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.VerifyAccount(aggId: Guid, comId: Guid, com: VerifyAccount) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.VerifyAccount, result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.ApproveAccount(aggId: Guid, comId: Guid, com: ApproveAccount) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.ApproveAccount, result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.LimitAccount(aggId: Guid, comId: Guid, com: LimitAccount) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof me.LimitAccount, result)
        }
