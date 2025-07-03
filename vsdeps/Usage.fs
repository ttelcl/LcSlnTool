// (c) 2025  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage focus =
  cp "\fovsdeps\f0: Tool to analyze Visual Studio solutions and project dependencies in them."
  cp ""
  cp "\fovsdeps \fysln \f0[\fg-f \fcfile.sln\f0|\fg-sln\f0] [\fg-tag \fc<tag>\f0]"
  cp "  Analyzes a solution file and the projects in it, emitting the results as data files."
  cp "  \fg-f \fcfile.sln\f0    The solution file to analyze"
  cp "  \fg-sln\fx\f0           Use the first solution file found in the current folder"
  cp "                \fx\fx\fx or an ancestor folder."
  cp "  \fg-tag \fc<tag>\f0     If given: include the \fctag\f0 in the output file names."
  cp "                \fx\fx\fx (preceded and followed by a '\fo.\f0')"
  cp ""
  cp "\fg-v               \f0Verbose mode"



