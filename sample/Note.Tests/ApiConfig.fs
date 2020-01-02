[<AutoOpen>]
module ApiConfig

open System
open UniStream.Infrastructure
open UniStream.Domain
open Note.Domain


let inline applyRaw aggregator = Aggregator.applyRaw aggregator
let inline applyCommand aggregator = Aggregator.applyCommand aggregator

let es = Uri "tcp://admin:changeit@localhost:4011"
let ld = Uri "tcp://admin:changeit@localhost:4012"
let lg = Uri "tcp://admin:changeit@localhost:4013"
let c1 = DomainEvent.create es
let c2 = DomainLog.create ld
let c3 = DiagnoseLog.create lg "Note App"
let get = DomainEvent.get c1
let esFunc = DomainEvent.write c1
let ldFunc = DomainLog.write c2
let lgFunc = DiagnoseLog.write c3
let cfg = Aggregator.config get esFunc ldFunc lgFunc

let actor = Aggregator.create<Actor.T> cfg 3L
let note = Aggregator.create<Note.T> cfg 3L