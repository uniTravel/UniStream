namespace Note.Domain


[<CLIMutable>]
type CreateActor = { Name: string }


[<RequireQualifiedAccess>]
module CreateActor =

    [<Sealed>]
    type T =
        static member ValueType : string
        member Apply : (Actor.T -> (string * byte[])[] * Actor.T)

    val create : (CreateActor -> T)