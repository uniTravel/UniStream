namespace Account.Sender.Controller

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain


[<ApiController>]
[<Route("[controller]/[action]")>]
type TransactionController
    (logger: ILogger<TransactionController>, [<FromKeyedServices(typeof<Transaction>)>] sender: ISender) as me =
    inherit ControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.InitPeriod(aggId: Guid, com: InitPeriod) =
        task {
            let! result = Sender.send sender aggId com
            return me.CreatedAtAction(nameof (me.InitPeriod), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.OpenPeriod(aggId: Guid, com: OpenPeriod) =
        task {
            let! result = Sender.send sender aggId com
            return me.CreatedAtAction(nameof (me.OpenPeriod), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.SetLimit(aggId: Guid, com: SetLimit) =
        task {
            let! result = Sender.send sender aggId com
            return me.CreatedAtAction(nameof (me.SetLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.ChangeLimit(aggId: Guid, com: ChangeLimit) =
        task {
            let! result = Sender.send sender aggId com
            return me.CreatedAtAction(nameof (me.ChangeLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.SetTransLimit(aggId: Guid, com: SetTransLimit) =
        task {
            let! result = Sender.send sender aggId com
            return me.CreatedAtAction(nameof (me.SetTransLimit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.Deposit(aggId: Guid, com: Deposit) =
        task {
            let! result = Sender.send sender aggId com
            return me.CreatedAtAction(nameof (me.Deposit), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.Withdraw(aggId: Guid, com: Withdraw) =
        task {
            let! result = Sender.send sender aggId com
            return me.CreatedAtAction(nameof (me.Withdraw), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.TransferOut(aggId: Guid, com: TransferOut) =
        task {
            let! result = Sender.send sender aggId com
            return me.CreatedAtAction(nameof (me.TransferOut), result)
        }

    [<HttpPost>]
    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member _.TransferIn(aggId: Guid, com: TransferIn) =
        task {
            let! result = Sender.send sender aggId com
            return me.CreatedAtAction(nameof (me.TransferIn), result)
        }
