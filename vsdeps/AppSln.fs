module AppSln

open System
open System.IO

open Newtonsoft.Json

open XsvLib

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

let private nullToEmpty (txt: string) =
  if txt = null then String.Empty else txt

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
    let projectCsvName = prefix + ".projects.csv"
    cp $"Saving \fg{projectCsvName}\f0."
    let sortedNodes = graph.TopologicallySorted
    do
      let columns =
        new XsvOutBuffer([
          "name"
          "index"
          "refcount"
          "depcount"
          "framework"
          "sdk"
          ])
      let colName = "name" |> columns.GetColumn
      let colSdk = "sdk" |> columns.GetColumn
      let colIndex = "index" |> columns.GetColumn
      let colFramework = "framework" |> columns.GetColumn
      let colRefCount = "refcount" |> columns.GetColumn
      let colDepCount = "depcount" |> columns.GetColumn
      use itrw = Xsv.WriteXsv(projectCsvName + ".tmp", 0)
      columns.EmitHeader itrw
      for node in sortedNodes do
        let framework =
          match node.Project.Content.Frameworks.Count with
          | 0 -> ""
          | 1 -> node.Project.Content.Frameworks[0]
          | _ -> "(multiple)"
        columns[colName] <- node.Label
        columns[colSdk] <- node.Project.Content.Sdk |> nullToEmpty
        columns[colIndex] <- node.TopoSortOrder |> string
        columns[colFramework] <- framework
        columns[colRefCount] <- node.DependsOn.Count |> string
        columns[colDepCount] <- node.DependentOf.Count |> string
        columns.Emit itrw
    projectCsvName |> finishFile
    let depCsvName = prefix + ".dependencies.csv"
    cp $"Saving \fg{depCsvName}\f0."
    do
      let columns =
        new XsvOutBuffer([
          "refFw"
          "ref"
          "refIdx"
          "asmFw"
          "asm"
          "asmIdx"
          ])
      let colAsm = "asm" |> columns.GetColumn
      let colRef = "ref" |> columns.GetColumn
      let colAsmFw = "asmFw" |> columns.GetColumn
      let colRefFw = "refFw" |> columns.GetColumn
      let colAsmIdx = "asmIdx" |> columns.GetColumn
      let colRefIdx = "refIdx" |> columns.GetColumn
      use itrw = Xsv.WriteXsv(depCsvName + ".tmp", 0)
      columns.EmitHeader itrw
      for asmNode in sortedNodes do
        for refNode in asmNode.DependsOn do
          let asmFw = String.Join("; ", asmNode.Project.Content.Frameworks)
          let refFw = String.Join("; ", refNode.Project.Content.Frameworks)
          columns[colAsm] <- asmNode.Label
          columns[colRef] <- refNode.Label
          columns[colAsmFw] <- asmFw
          columns[colRefFw] <- refFw
          columns[colAsmIdx] <- asmNode.TopoSortOrder |> string
          columns[colRefIdx] <- refNode.TopoSortOrder |> string
          columns.Emit itrw
    depCsvName |> finishFile
    do
      let fulldotName = prefix + ".full.dot"
      cp $"Saving \fg{fulldotName}\f0."
      graph.SaveDotFile(fulldotName + ".tmp", false, false)
      fulldotName |> finishFile
      cp $"   Reminder: use \fydot -Tsvg -O {fulldotName}\f0 to generate SVG from this file"
    //do
    //  let puredotName = prefix + ".pure.dot"
    //  cp $"Saving \fg{puredotName}\f0."
    //  graph.SaveDotFile(puredotName + ".tmp", true, false)
    //  puredotName |> finishFile
    //  cp $"   Reminder: use \fydot -Tsvg -O {puredotName}\f0 to generate SVG from this file"
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


