[<AutoOpen>]
module ApiConfig

open System
open UniStream.Infrastructure
open UniStream.Domain
open Note.Domain.ActorAgg
open Note.Domain.NoteAgg


let inline applyRaw aggregator = Aggregator.applyRaw aggregator
let inline applyCommand aggregator = Aggregator.applyCommand aggregator

let uri = Uri "tcp://admin:changeit@localhost:1113"
let c1 = DomainEvent.create uri
let c2 = DomainLog.create uri
let c3 = DiagnoseLog.create uri "Note App"
let get = DomainEvent.get c1
let esFunc = DomainEvent.write c1
let ldFunc = DomainLog.write c2
let lgFunc = DiagnoseLog.write c3
let cfg = Aggregator.config get esFunc ldFunc lgFunc

let actor = Aggregator.create<Actor.T> cfg 3L
let note = Aggregator.create<Note.T> cfg 3L