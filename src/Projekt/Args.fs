module Projekt.Args

open Projekt.Types
open System.IO
open Nessos.UnionArgParser

type private Args =
    | Template of string
    | FrameworkVersion of string
    | Direction of string
    | Repeat of int
    | Link of string
    | Compile of string
with
    interface IArgParserTemplate with
        member s.Usage = 
            match s with
            | Template _ -> "init -- specify the template (library|console) [default: library]"
            | Direction _ -> "movefile -- specify the direction (down|up)"
            | Repeat _ -> "movefile -- specify the distance [default: 1]"
            | FrameworkVersion _ -> "init-- specify the framework version (4.0|4.5|4.5.1) [default: 4.5]"
            | Link _ -> "addfile -- specify an optional Link attribute"
            | Compile _ -> "addfile -- should the file be compiled or not (false|true) [default: true]"

let private templateArg (res : ArgParseResults<Args>) =
    match res.TryGetResult(<@ Template @>) with
    | Some (ToLower "console") -> Console
    | Some (ToLower "library") -> Library
    | None -> Library
    | _ -> failwith "invalid template argument specified"

let private parseDirection s =
    match s with
    | ToLower "up" -> Up
    | ToLower "down" -> Down
    | _ -> failwith "invalid direction specified"

let private parseCompile s =
    match s with
    | ToLower "true" -> true
    | ToLower "false" -> false
    | _ -> failwith "invalid compilation option"

let private parser = UnionArgParser.Create<Args>()

let private (|Options|) (args : string list) =
    let results = parser.Parse(List.toArray args)
    results

let (|FullPath|_|) (path : string) =
    try 
        Path.GetFullPath path |> Some
    with
    | _ -> None

let commandUsage = "projekt (init|reference|newfile|addfile) /path/to/project [/path/to/file]"

let parse (ToList args) : Result<Operation> =
    try
        match args with
        | ToLower "init" :: FullPath path :: Options opts -> 
            let template = templateArg opts
            Init (ProjectInitData.create (path, template)) |> Success
            
        | ToLower "addfile" :: FullPath project :: FullPath file :: Options opts ->
            let compile = not (opts.Contains <@ Compile @>) ||
                          opts.PostProcessResult(<@ Compile @>, parseCompile)
            AddFile { ProjPath = project
                      FilePath = file
                      Link = opts.TryGetResult <@ Link @>
                      Compile = compile }
            |> Success
            
        | [ToLower "delfile"; FullPath project; FullPath file] -> 
            DelFile { ProjPath = project; FilePath = file }
            |> Success
            
        | ToLower "movefile" :: FullPath project :: FullPath file :: Options opts
                when opts.Contains <@ Direction @> ->

            let direction = opts.PostProcessResult(<@ Direction @>, parseDirection)
            MoveFile { ProjPath = project
                       FilePath = file
                       Direction = direction
                       Repeat = opts.GetResult(<@ Repeat @>, 1)}
            |> Success
            
        | [ToLower "reference"; FullPath project; FullPath reference] -> 
            Reference { ProjPath = project; Reference = reference } |> Success
        | _ -> Failure (parser.Usage (sprintf "Error: '%s' is not a recognized command or received incorrect arguments.\n\n%s" args.Head commandUsage))
    with
      | :? System.ArgumentException as e ->
            let lines = e.Message.Split([|'\n'|])
            let msg = parser.Usage (sprintf "%s\n\n%s" lines.[0] commandUsage)
            Failure msg
