[<AutoOpen>]
module ApiConfig

open System
open Note.Application


let es = Uri "tcp://admin:changeit@localhost:4011"
let ld = Uri "tcp://admin:changeit@localhost:4012"
let lg = Uri "tcp://admin:changeit@localhost:4013"
let app = AppService (es, ld, lg)