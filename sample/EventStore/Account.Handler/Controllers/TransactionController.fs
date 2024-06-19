namespace Account.Api.Controllers

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Account.Application
open Account.Domain


[<ApiController>]
[<Route("[controller]/[action]/{comId}")>]
type TransactionController(logger: ILogger<TransactionController>, svc: TransactionService) as me =
    inherit ControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.InitPeriod(aggId: Guid, comId: Guid, com: InitPeriod) =
        task {
            let! result = svc.InitPeriod aggId comId com
            return me.CreatedAtAction(nameof (me.InitPeriod), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.OpenPeriod(aggId: Guid, comId: Guid, com: OpenPeriod) =
        task {
            let! result = svc.OpenPeriod aggId comId com
            return me.CreatedAtAction(nameof (me.OpenPeriod), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.SetLimit(aggId: Guid, comId: Guid, com: SetLimit) =
        task {
            let! result = svc.SetLimit aggId comId com
            return me.CreatedAtAction(nameof (me.SetLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.ChangeLimit(aggId: Guid, comId: Guid, com: ChangeLimit) =
        task {
            let! result = svc.ChangeLimit aggId comId com
            return me.CreatedAtAction(nameof (me.ChangeLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.SetTransLimit(aggId: Guid, comId: Guid, com: SetTransLimit) =
        task {
            let! result = svc.SetTransLimit aggId comId com
            return me.CreatedAtAction(nameof (me.SetTransLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.Deposit(aggId: Guid, comId: Guid, com: Deposit) =
        task {
            let! result = svc.Deposit aggId comId com
            return me.CreatedAtAction(nameof (me.Deposit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.Withdraw(aggId: Guid, comId: Guid, com: Withdraw) =
        task {
            let! result = svc.Withdraw aggId comId com
            return me.CreatedAtAction(nameof (me.Withdraw), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.TransferOut(aggId: Guid, comId: Guid, com: TransferOut) =
        task {
            let! result = svc.TransferOut aggId comId com
            return me.CreatedAtAction(nameof (me.TransferOut), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.TransferIn(aggId: Guid, comId: Guid, com: TransferIn) =
        task {
            let! result = svc.TransferIn aggId comId com
            return me.CreatedAtAction(nameof (me.TransferIn), result)
        }
