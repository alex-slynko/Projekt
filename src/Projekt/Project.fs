module Projekt.Project

open System
open System.Xml.Linq

let xns s = XNamespace.Get s
let msbuildns = "{http://schemas.microsoft.com/developer/msbuild/2003}"
let xname s = XName.Get s

let xn s = XName.Get s
let xe n (v: obj) = new XElement(xn (msbuildns + n), v)
let xa n (v: obj) = new XAttribute(xn n, v)

let (|Head|_|) =
    Seq.tryFind (fun _ -> true)

let (|Value|) (xe: XElement) =
    xe.Value

let (|Guid|_|) s =
    match Guid.TryParse s with
    | true, g -> Some g
    | _ -> None

let (|Descendant|_|) name (xe : XElement) =
    match xe.Descendants (xn (msbuildns + name)) with
    | Head h -> Some h
    | _ -> None

let (|Element|_|) name (xe : XElement) =
    match xe.Element (xn (msbuildns + name)) with
    | null -> None 
    | e -> Some e

//queries
let internal projectGuid = 
    function
    | Descendant "ProjectGuid" (Value (Guid pg)) -> 
        Some pg 
    | _ -> None

let internal projectName = 
    function
    | Descendant "Name" (Value name) -> 
        Some name 
    | _ -> None

let internal projectReferenceItemGroup =
    function
    | Descendant "ProjectReference" e -> 
        e.Parent |> Some
    | _ -> None

let internal addProjRefNode (path: string) (name: string) (guid : Guid) (el: XElement) =
    match projectReferenceItemGroup el with
    | Some prig ->
        //TODO check to ensure duplicate ProjectReferences aren't added
        prig.Add(
            xe "ProjectReference"
                [ xa "Include" path |> box
                  xe "Name" name |> box
                  xe "Project" (sprintf "{%O}" <| guid) |> box
                  xe "Private" "True" |> box ] )
        prig
    | None -> failwith "TODO add ItemGroup node for add reference"

let addReference (project : string) (reference : string) =
    let relPath = Projekt.Util.makeRelativePath project reference
    let proj = XElement.Load project
    let reference = XElement.Load reference
    let name = projectName reference
    let guid = projectGuid reference
    addProjRefNode relPath name.Value guid.Value proj
    




