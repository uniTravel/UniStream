[<AutoOpen>]
module ApiConfig

open UniStream.Infrastructure
open UniStream.Domain
open Note.Domain.ActorAgg
open Note.Domain.NoteAgg


let inline applyRaw aggregator = Aggregator.applyRaw aggregator
let inline applyCommand aggregator = Aggregator.applyCommand aggregator
let cfg = Aggregator.config EventStore.getAgg EventStore.getFromAgg EventStore.esWrite EventStore.ldWrite EventStore.lgWrite

let actor = Aggregator.create<Actor.T> cfg 3L
let note = Aggregator.create<Note.T> cfg 3L