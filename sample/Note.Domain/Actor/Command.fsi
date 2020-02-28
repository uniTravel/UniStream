namespace Note.Domain

open Note.Contract


[<RequireQualifiedAccess>]
module CreateActor =

    [<Sealed>]
    type T =
        static member ValueType : string
        member Apply : (Actor.T -> (string * byte[])[] * Actor.T)

    val create : (CreateActor -> T)