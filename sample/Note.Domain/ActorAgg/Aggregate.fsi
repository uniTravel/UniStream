namespace Note.Domain.ActorAgg


[<CLIMutable>]
type Create = { Name: string }


[<RequireQualifiedAccess>]
module Actor =

    type T =
        | Init
        | Active of {| Name: string |}

    val inline internal actorCreated : ^c -> T -> T
        when ^c : (member Value: Create)