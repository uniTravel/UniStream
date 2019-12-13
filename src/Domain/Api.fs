namespace UniStream.Domain

open System


module Api =

    type T = {
        _get: string -> Guid -> (byte[] * byte[])[]
        _esFunc: string -> Guid -> string -> byte[] -> byte[] -> unit
        _ldFunc: string -> Guid -> string -> byte[] -> byte[] -> unit
        _lgFunc: string -> byte[] -> unit
        _blockSeconds: int64
    } with
        member this.Get = this._get
        member this.EsFunc = this._esFunc
        member this.LdFunc = this._ldFunc
        member this.LgFunc = this._lgFunc
        member this.BlockSeconds = this._blockSeconds

    let create get esFunc ldFunc lgFunc blockSeconds =
        { _get = get; _esFunc = esFunc; _ldFunc = ldFunc; _lgFunc = lgFunc; _blockSeconds = blockSeconds }

    let inline applyCommand  (cfg: T) metaTrace command = async {
        let aggregator = Aggregator.create<'agg> cfg.Get cfg.EsFunc cfg.LdFunc cfg.LgFunc cfg.BlockSeconds
        do! Aggregator.applyCommand aggregator metaTrace command
    }

    let inline applyRaw (cfg: T) aggId cBytes f = async {
        let aggregator = Aggregator.create<'agg> cfg.Get cfg.EsFunc cfg.LdFunc cfg.LgFunc cfg.BlockSeconds
        do! Aggregator.applyRaw aggregator aggId cBytes f
    }