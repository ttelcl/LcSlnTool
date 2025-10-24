// (c) 2025  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage focus =
  if focus = "" then
    cp "\fovsdeps\f0: Tool to analyze Visual Studio solutions and project dependencies in them."
    cp ""
  if focus = "" || focus = "sln" then
    cp "\fovsdeps \fysln \f0[\fg-f \fcfile.sln\f0|\fg-sln\f0] [\fg-tag \fc<tag>\f0]"
    cp "  Analyzes a solution file and the projects in it, emitting the results as data files."
    cp "  \fg-f \fcfile.sln\f0    The solution file to analyze"
    cp "  \fg-sln\fx\f0           Use the first solution file found in the current folder"
    cp "                \fx\fx\fx or an ancestor folder."
    cp "  \fg-tag \fc<tag>\f0     If given: include the \fctag\f0 in the output file names."
    cp "                \fx\fx\fx (preceded and followed by a '\fo.\f0')"
    cp ""
  if focus = "" || focus = "scan" then
    cp "\fovsdeps \fyscan \fg-d \fc<directory>\f0 [\fg-tag \fc<tag>\f0]"
    cp "  Look for \fo*.sln\f0, \fo*.csproj\f0 and \fo*.fsproj\f0 file in the specified"
    cp "  directory and its descendent directories, and map relations between solution"
    cp "  and projects, and in between projects."
    cp "  \fg-d \fc<root>\f0      The root of the directory tree to scan"
    cp "  \fg-tag \fc<tag>\f0     Used to construct output file names. " 
    cp "  \fx     \fx     \fx     Defaults to the final segment of the root directory"
    cp ""
  cp "\fg-v               \f0Verbose mode"



