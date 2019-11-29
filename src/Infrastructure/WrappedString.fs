namespace UniStream.Infrastructure

module WrappedString =

    type IWrappedString =
        abstract Value : string

    let create canonicalize isValid ctor (s:string) =
        if isNull s
        then None
        else
            let s' = canonicalize s
            if isValid s'
            then Some (ctor s')
            else None

    let apply f (s: IWrappedString) =
        s.Value |> f

    let value s = apply id s

    let equals left right =
        (value left) = (value right)

    let compareTo left right =
        (value left).CompareTo (value right)

    let singleLineTrimmed s =
        System.Text.RegularExpressions.Regex.Replace(s,"\s"," ").Trim()

    let lengthValidator len (s:string) =
        s.Length <= len

    type String100 = String100 of string with
        interface IWrappedString with
            member this.Value = let (String100 s) = this in s

    let string100 =
        create singleLineTrimmed (lengthValidator 100) String100

    let convertTo100 s =
        apply string100 s

    type String50 = String50 of string with
        interface IWrappedString with
            member this.Value = let (String50 s) = this in s

    let string50 =
        create singleLineTrimmed (lengthValidator 50) String50

    let convertTo50 s =
        apply string50 s

    let mapAdd k v map =
        Map.add (value k) v map

    let mapContainsKey k map =
        Map.containsKey (value k) map

    let mapTryFind k map =
        Map.tryFind (value k) map

module EmailAddress =

    type T = EmailAddress of string with
        interface WrappedString.IWrappedString with
            member this.Value = let (EmailAddress s) = this in s

    let create =
        let canonicalize = WrappedString.singleLineTrimmed
        let isValid s =
            (WrappedString.lengthValidator 100 s) &&
            System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$")
        WrappedString.create canonicalize isValid EmailAddress

    let convert s = WrappedString.apply create s
