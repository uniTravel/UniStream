namespace Note.Domain


[<CLIMutable>]
type CreateActor = { Name: string }


[<RequireQualifiedAccess>]
module CreateActor =

    [<Sealed>]
    type T =
        static member ValueType : string
        member Apply : (Actor.T -> byte[] -> Result<(string * byte[] * byte[]) seq * Actor.T, string>)

    val create : (CreateActor -> T)