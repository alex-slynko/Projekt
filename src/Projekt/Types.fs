[<AutoOpen>]
module Projekt.Types

type Template =
    | Console
    | Library

type Direction =
    | Up
    | Down

type FrameworkVersion =
    | V4_0
    | V4_5
    | V4_5_1
with 
    override x.ToString () =
        match x with
        | V4_0 -> "4.0"
        | V4_5 -> "4.5"
        | V4_5_1 -> "4.5.1"

type ProjectInitData =
    { ProjPath: string
      Template: Template
      FrameworkVersion: FrameworkVersion
      Solution: string option }
with 
    static member create (path, ?template, ?fxversion, ?solution) =
        { ProjPath = path
          Template = defaultArg template Library
          FrameworkVersion = defaultArg fxversion V4_5
          Solution = solution }

type ProjectReferenceData =
    { ProjPath: string
      Reference: string }

type AddFileData =
    { ProjPath: string
      FilePath: string
      Link: Option<string>
      Compile: bool }
    
type DelFileData =
    { ProjPath: string
      FilePath: string }

type MoveFileData =
    { ProjPath: string
      FilePath: string
      Direction: Direction
      Repeat: int }

type Operation =
    | Init of ProjectInitData //project path 
    | Reference of ProjectReferenceData
    | AddFile of AddFileData
    | DelFile of DelFileData
    | MoveFile of MoveFileData
    | Exit

type Result<'a> =
    | Success of 'a
    | Failure of string

type ResultBuilder() =
    member __.Return (x) =
        Success x
    member __.ReturnFrom (x : Result<'T>) = x
    member __.Bind(m, f) =
        match m with
        | Success m -> f m
        | Failure s -> Failure s
    member __.Delay f = f
    member __.Run f = f ()
    member __.Zero () = Success ()
    member __.TryWith (body, handler) =
        try
            body()
        with
        | e -> handler e
    member __.TryFinally (body, compensation) =
        try
            body()
        finally
            compensation()
    member x.Using(d:#System.IDisposable, body) =
        let result = fun () -> body d
        x.TryFinally (result, fun () ->
            match d with
            | null -> ()
            | d -> d.Dispose())
    member x.While (guard, body) =
        if not <| guard () then
            x.Zero()
        else
            x.Bind (body(), (fun () -> x.While(guard, body)))
    member x.For(s:seq<_>, body) =
        x.Using(s.GetEnumerator(), fun enum ->
            x.While(enum.MoveNext,
                x.Delay(fun () -> body enum.Current)))
let result = ResultBuilder()


