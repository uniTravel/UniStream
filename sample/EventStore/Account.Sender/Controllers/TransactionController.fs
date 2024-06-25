namespace Account.Sender.Controller

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain


[<ApiController>]
[<Route("[controller]/[action]/{comId}")>]
type TransactionController(logger: ILogger<TransactionController>, sender: ISender<Transaction>) as me =
    inherit ControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.InitPeriod(aggId: Guid, comId: Guid, com: InitPeriod) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.InitPeriod), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.OpenPeriod(aggId: Guid, comId: Guid, com: OpenPeriod) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.OpenPeriod), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.SetLimit(aggId: Guid, comId: Guid, com: SetLimit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.SetLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.ChangeLimit(aggId: Guid, comId: Guid, com: ChangeLimit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.ChangeLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.SetTransLimit(aggId: Guid, comId: Guid, com: SetTransLimit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.SetTransLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.Deposit(aggId: Guid, comId: Guid, com: Deposit) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.Deposit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.Withdraw(aggId: Guid, comId: Guid, com: Withdraw) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.Withdraw), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.TransferOut(aggId: Guid, comId: Guid, com: TransferOut) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.TransferOut), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.TransferIn(aggId: Guid, comId: Guid, com: TransferIn) =
        task {
            let! result = Sender.send sender aggId comId com
            return me.CreatedAtAction(nameof (me.TransferIn), result)
        }
