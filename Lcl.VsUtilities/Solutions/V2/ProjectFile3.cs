/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Lcl.VsUtilities.Solutions.V2;

/// <summary>
/// Project file information, used in a project-oriented view
/// </summary>
public class ProjectFile3
{
  /// <summary>
  /// Create a new ProjectFile3
  /// </summary>
  public ProjectFile3(
    string fullpath,
    string name,
    IEnumerable<SolutionInfo3> solutions)
  {
    FullPath = fullpath;
    Name = name;
    Solutions = solutions.ToList();
  }

  /// <summary>
  /// The full path to the project file
  /// </summary>
  [JsonProperty("fullpath")]
  public string FullPath { get; }

  /// <summary>
  /// The project name.
  /// </summary>
  [JsonProperty("name")]
  public string Name { get; }

  /// <summary>
  /// The list of solutions that point to this project
  /// </summary>
  [JsonProperty("solutions")]
  public IReadOnlyList<SolutionInfo3> Solutions { get; }

}
