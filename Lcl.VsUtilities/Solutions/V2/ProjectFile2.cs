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

using Lcl.VsUtilities.VirtualPaths;

using Newtonsoft.Json;

namespace Lcl.VsUtilities.Solutions.V2;

/// <summary>
/// Revised "project file" pointer, used as component in a
/// solution-oriented view.
/// </summary>
public class ProjectFile2
{
  /// <summary>
  /// Originally the Deserialization constructor.
  /// Use <see cref="TryCreate"/> to invoke in actual creation situations.
  /// Deserialization is actually no longer supported.
  /// </summary>
  public ProjectFile2(
    string fullpath,
    string label,
    string solutionid,
    VirtualPath vpath)
  {
    Label = label;
    SolutionId = solutionid;
    VPath = vpath;
    Name = Path.GetFileNameWithoutExtension(VPath.VPath);
    FullPath = fullpath;
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
    var vpdb = sf.VpDb;
    if(vpdb == null)
    {
      throw new NotSupportedException(
        "The current implementation requires virtual path mappings");
    }
    var vpath = vpdb.MatchPath(projectPath);
    if(vpath == null)
    {
      throw new NotSupportedException(
        $"Expecting project file paths to be mappable to virtual paths. Cannot map '{projectPath}'");
    }
    return new ProjectFile2(
      projectPath,
      spi.Label,
      sf.Id,
      vpath
    );
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
  /// The full path information, but mapped to a virtual path pair
  /// </summary>
  [JsonProperty("vpath")]
  public VirtualPath VPath { get; }

  /// <summary>
  /// The project name, derived from <see cref="VPath"/>
  /// Expected to be the same as <see cref="Label"/>
  /// </summary>
  [JsonProperty("name")]
  public string Name { get; }

  /// <summary>
  /// The full path name. (Used in the process of linking references.)
  /// </summary>
  [JsonIgnore]
  public string FullPath { get; }
}

