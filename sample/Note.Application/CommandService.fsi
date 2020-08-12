namespace Note.Application

open System
open UniStream.Domain
open Note.Contract
open Note.Domain


/// <summary>命令服务模块
/// <para>应用的命令服务内部实现，业务流程仅涉及单个聚合。</para>
/// </summary>
[<RequireQualifiedAccess>]
module internal CommandService =

    val createActor :Immutable.T<Actor.T> -> string -> string -> string -> CreateActor -> Async<Actor>

    val createNote : Mutable.T<Note.T> -> string -> string -> string -> CreateNote -> Async<Note>

    val changeNote : Mutable.T<Note.T> -> string -> string -> string -> ChangeNote -> Async<Note>

    val appendNote : Observer.T<NoteObserver.T> -> string -> uint64 -> string -> ReadOnlyMemory<byte> -> Async<unit>