namespace Note.Domain

open UniStream.Domain
open Note.Contract


module CreateNote =
    type T = CreateNote of CreateNote with
        static member ValueType = typeof<CreateNote>.FullName
        member this.Apply =
            let cv = let (CreateNote c) = this in c
            Note.createNote { Title = cv.Title; Content = cv.Content }
    let isValid _ = true
    let create = Command.create isValid CreateNote

module ChangeNote =
    type T = ChangeNote of ChangeNote with
        static member ValueType = typeof<ChangeNote>.FullName
        member this.Value = let (ChangeNote c) = this in c
        member this.Apply =
            let cv = let (ChangeNote c) = this in c
            Note.changeNote { Content = cv.Content }
    let isValid _ = true
    let create = Command.create isValid ChangeNote