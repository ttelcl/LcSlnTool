module AppSln

open System
open System.IO

open Newtonsoft.Json

open Lcl.VsUtilities.Solutions

open ColorPrint
open CommonTools

type private SolutionSource =
  | Search
  | SolutionFile of string

type private Options = {
  Source: SolutionSource option
  Tag: string
}

let rec searchSolution folder =
  let files = Directory.GetFiles(folder, "*.sln")
  if files.Length > 2 then
    cp "\foSolution file search is ambiguous\f0:"
    for file in files do
      cp $"    \fy{file}\f0"
    None
  elif files.Length = 1 then
    files[0] |> Some
  else
    let parent = Path.GetDirectoryName(folder)
    if parent |> String.IsNullOrEmpty then
      cp "\foNo solution file found in the current directory or any parent\f0."
      None
    else
      parent |> searchSolution

let private runSln o =
  let slnfile =
    match o.Source with
    | None ->
      None
    | Some(Search) ->
      Environment.CurrentDirectory |> searchSolution
    | Some(SolutionFile(f)) ->
      Some(f)
  match slnfile with
  | None ->
    1
  | Some(slnfile) ->
    cp $"Loading solution file \fg{slnfile}\f0."
    let sln = new Solution(slnfile)
    let tree = sln.Info.BuildSolutionTree()
    let graph = new ProjectDependencyGraph(sln)
    graph.StripSingletonStubs() |> ignore
    let summaries = graph |> sln.BuildProjectSummaries
    let prefix =
      if o.Tag |> String.IsNullOrEmpty then
        sln.Info.Name
      else
        sln.Info.Name + "." + o.Tag
    do
      let jsonName = prefix + ".slninfo.json"
      let json = JsonConvert.SerializeObject(summaries, Formatting.Indented)
      cp $"Saving \fg{jsonName}\f0."
      File.WriteAllText(jsonName + ".tmp", json)
      jsonName |> finishFile
    do
      let jsonName = prefix + ".slntree.json"
      let json = JsonConvert.SerializeObject(tree, Formatting.Indented)
      cp $"Saving \fg{jsonName}\f0."
      File.WriteAllText(jsonName + ".tmp", json)
      jsonName |> finishFile
    0

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
    | "-f" :: slnfile :: rest ->
      rest |> parsemore {o with Source = slnfile |> SolutionFile |> Some}
    | "-sln" :: rest ->
      rest |> parsemore {o with Source = Search |> Some}
    | "-tag" :: tag :: rest ->
      rest |> parsemore {o with Tag = tag}
    | [] ->
      if o.Source.IsNone then
        cp "\frNo solution specified\f0 (\fg-f\f0 or \fg-sln\f0 option)"
        cp ""
        None
      else
        o |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \fy{x}\f0."
      cp ""
      None
  let oo = args |> parsemore {
    Source = None
    Tag = null
  }
  match oo with
  | Some(o) ->
    o |> runSln
  | None ->
    Usage.usage "sln"
    1


