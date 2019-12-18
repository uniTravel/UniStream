[<AutoOpen>]
module ApiConfig

open UniStream.Infrastructure
open UniStream.Domain


let api = Api.create EventStore.getAgg EventStore.esWrite EventStore.ldWrite EventStore.lgWrite 3L

let inline applyRaw aggId = Api.applyRaw api aggId

let inline applyCommand meta = Api.applyCommand api meta