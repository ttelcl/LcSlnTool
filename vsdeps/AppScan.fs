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

let private runScan o =
  let prefix = Environment.CurrentDirectory
  let slnFileFromFile (fileName:string) =
    new SolutionFile(fileName, prefix)
  let slnFiles =
    FileFinder.FindFilesRecursive(o.Root, "*.sln")
    |> Seq.map slnFileFromFile
    |> Seq.toArray
    |> Array.sortBy(fun sf -> $"{sf.SolutionName} ! {sf.UiFullName}".ToLowerInvariant())
  
  for slnFile in slnFiles do
    slnFile.Load()

  let supportedSolutions =
    slnFiles |> Array.where(fun sf -> sf.HasSupportedProjects)
  
  cp $"Found \fc{supportedSolutions.Length}\f0 / \fb{slnFiles.Length}\f0 solution files:"
  
  let grouped =
    supportedSolutions // slnFiles
    |> Array.groupBy (fun sf -> sf.SolutionName.ToLowerInvariant())
  
  for (_, solutions) in grouped do
    for (i,slnFile) in solutions |> Seq.indexed do
      if solutions.Length <> 1 then
        slnFile.Index <- i+1;
    
  let solutionsCsvName = $"{o.Tag}.solutions.csv"
  do
    use csv = solutionsCsvName |> startFile
    csv.WriteLine("id,prefix,solution,index,count")
    for (_, solutions) in grouped do
      if solutions.Length = 1 then
        let slnFile = solutions[0]
        let hasSupportedProjects = slnFile.HasSupportedProjects
        let color = if hasSupportedProjects then "\fg" else "\fk"
        cpx $"  {color}{slnFile.Id}\f0 :"
        cpx $" \fb{slnFile.ProjectCount} \f0/\fc {slnFile.SupportedProjectCount}"
        cp $" \f0(\fk{slnFile.UiFullName}\f0)"
        csv.WriteLine($"{slnFile.Id},{slnFile.Prefix},{slnFile.UiFullName},0,{slnFile.ProjectCount}")
      else
        for (i,slnFile) in solutions |> Seq.indexed do
          let hasSupportedProjects = slnFile.HasSupportedProjects
          let color = if hasSupportedProjects then "\fg" else "\fk"
          cpx $"  {color}{slnFile.Id}\fy#{i+1}\f0 :"
          cpx $" \fb{slnFile.ProjectCount} \f0/\fc {slnFile.SupportedProjectCount}"
          cp $" \f0(\fk{slnFile.UiFullName}\f0)"
          csv.WriteLine($"{slnFile.Id},{slnFile.Prefix},{slnFile.UiFullName},{i+1},{slnFile.ProjectCount}")
  solutionsCsvName |> finishFile

  let solutionsJsonName = $"{o.Tag}.solutions.json"
  do
    use jw = solutionsJsonName |> startFile
    let json = JsonConvert.SerializeObject(supportedSolutions, Formatting.Indented)
    jw.WriteLine(json)
  solutionsJsonName |> finishFile
  cp ""
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
          let tag = (new DirectoryInfo(o.Root)).Name
          {o with Tag = tag} |> Some
        else
          o |> Some
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
  

