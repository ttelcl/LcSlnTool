module AppScan

open System
open System.IO

open Newtonsoft.Json

open XsvLib

open Lcl.VsUtilities.Solutions
open Lcl.VsUtilities.Solutions.V2

open ColorPrint
open CommonTools

type private ScanOptions = {
  Tag: string
  Root: string
}

type private SlnFile = {
  FilePath: string
  Name: string
}


let private runScan o =
  let prefixLength = Environment.CurrentDirectory.Length
  let slnFileFromFile (fileName:string) =
    {
      FilePath = fileName.Substring(prefixLength)
      Name = fileName |> Path.GetFileNameWithoutExtension
    }
  let slnFiles =
    FileFinder.FindFilesRecursive(o.Root, "*.sln")
    |> Seq.map slnFileFromFile
    |> Seq.toArray
    |> Array.sortBy(fun sf -> $"{sf.Name} ! {sf.FilePath}".ToLowerInvariant())
  cp $"Found \fb{slnFiles.Length}\f0 solution files:"
  for slnFile in slnFiles do
    cp $"  \fg{slnFile.Name}\f0 (\fk{slnFile.FilePath}\f0)"
  cp "\frNYI\f0."
  1

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _
    | "-help" :: _
    | "--help" :: _ ->
      None
    | "-d" :: directory :: rest ->
      if directory |> Directory.Exists |> not then
        cp $"\frError: directory not found: \fo{directory}\f0."
        cp ""
        None
      else
        rest |> parsemore {o with Root = directory |> Path.GetFullPath}
    | "-tag" :: tag :: rest ->
      rest |> parsemore {o with Tag = tag}
    | [] ->
      if o.Root |> String.IsNullOrEmpty then
        cp "\frNo root directory specified\f0 (\fg-d\f0 option)"
        cp ""
        None
      else
        if o.Tag |> String.IsNullOrEmpty then
          o |> Some
        else
          let tag = o.Root |> Path.GetFileName
          {o with Tag = tag} |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \fy{x}\f0."
      cp ""
      None
  let oo = args |> parsemore {
    Tag = null
    Root = null
  }
  match oo with
    | None ->
      Usage.usage "scan"
      1
    | Some(o) ->
      o |> runScan
  

