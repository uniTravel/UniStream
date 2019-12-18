namespace UniStream.Domain

open System


module Api =

    type T =
        { Get: string -> Guid -> (byte[] * byte[])[]
          EsFunc: string -> Guid -> string -> byte[] -> byte[] -> unit
          LdFunc: string -> Guid -> string -> byte[] -> byte[] -> unit
          LgFunc: string -> byte[] -> unit
          BlockSeconds: int64 }

    let create get esFunc ldFunc lgFunc blockSeconds =
        { Get = get; EsFunc = esFunc; LdFunc = ldFunc; LgFunc = lgFunc; BlockSeconds = blockSeconds }

    let inline applyCommand (cfg: T) metaTrace command = async {
        let aggregator = Aggregator.create<'agg> cfg.Get cfg.EsFunc cfg.LdFunc cfg.LgFunc cfg.BlockSeconds
        do! Aggregator.applyCommand aggregator metaTrace command
    }

    let inline applyRaw (cfg: T) aggId cBytes f = async {
        let aggregator = Aggregator.create<'agg> cfg.Get cfg.EsFunc cfg.LdFunc cfg.LgFunc cfg.BlockSeconds
        do! Aggregator.applyRaw aggregator aggId cBytes f
    }