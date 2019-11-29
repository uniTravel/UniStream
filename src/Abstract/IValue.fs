namespace UniStream.Abstract


type IValue =
    interface end


type IWrapped<'v when 'v :> IValue> =
    abstract member Value : 'v