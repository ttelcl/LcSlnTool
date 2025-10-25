/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Lcl.VsUtilities.Solutions.V2;

/// <summary>
/// Revised "project file" pointer, used as component in a
/// solution-oriented view.
/// </summary>
public class ProjectFile2
{
  /// <summary>
  /// Deserialization constructor. Use <see cref="TryCreate"/> to invoke
  /// in actual creation situations.
  /// </summary>
  [JsonConstructor]
  public ProjectFile2(
    string fullpath,
    string label,
    string solutionid)
  {
    FullPath = Path.GetFullPath(fullpath);
    Label = label;
    SolutionId = solutionid;
    Name = Path.GetFileNameWithoutExtension(fullpath);
  }

  /// <summary>
  /// Create a <see cref="ProjectFile2"/>, if the arguments allow
  /// </summary>
  /// <param name="sf"></param>
  /// <param name="spi"></param>
  /// <returns></returns>
  public static ProjectFile2? TryCreate(
    SolutionFile sf,
    SolutionProjectInfo spi)
  {
    if(String.IsNullOrEmpty(spi.Path))
    {
      // not a supported project for this class
      return null;
    }
    var solutionFolder = Path.GetDirectoryName(sf.FullName);
    if(String.IsNullOrEmpty(solutionFolder))
    {
      throw new ArgumentException(
        "Not expecting solution folder to be null");
    }
    var projectPath = Path.Combine(solutionFolder, spi.Path);
    if(!File.Exists(projectPath))
    {
      return null;
    }
    return new ProjectFile2(
      projectPath,
      spi.Label,
      sf.Id);
  }

  /// <summary>
  /// The project label as found in the solution file.
  /// Expected to be the same as <see cref="Name"/>
  /// </summary>
  [JsonProperty("label")]
  public string Label { get; }

  /// <summary>
  /// The solution ID
  /// </summary>
  [JsonProperty("solutionid")]
  public string SolutionId { get; }

  /// <summary>
  /// The full path (reconstructed from the solution path and relative project path)
  /// </summary>
  [JsonProperty("fullpath")]
  public string FullPath { get; }

  /// <summary>
  /// The project name, derived from <see cref="FullPath"/>
  /// Expected to be the same as <see cref="Label"/>
  /// </summary>
  [JsonProperty("name")]
  public string Name { get; }
}

