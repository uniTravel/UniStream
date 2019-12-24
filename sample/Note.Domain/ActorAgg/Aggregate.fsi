namespace Note.Domain.ActorAgg


[<CLIMutable>]
type ActorCreated = { Name: string }


[<RequireQualifiedAccess>]
module Actor =

    type T

    val internal actorCreated : ActorCreated -> T -> T

    type T with
        static member Empty : T
        member Apply : (string -> byte[] -> T)