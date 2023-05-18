namespace UniStream.Domain


module Spec =

    let inline a<'agg when 'agg :> Aggregate> ([<InlineIfLambda>] left) ([<InlineIfLambda>] right) (obj: 'agg) =
        left obj && right obj

    let inline o<'agg when 'agg :> Aggregate> ([<InlineIfLambda>] left) ([<InlineIfLambda>] right) (obj: 'agg) =
        left obj || right obj

    let inline n<'agg when 'agg :> Aggregate> ([<InlineIfLambda>] spec) (obj: 'agg) = not <| spec obj
