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

using Lcl.VsUtilities.VirtualPaths;

namespace Lcl.VsUtilities.Solutions.V2;

/// <summary>
/// Reference to a solution file
/// </summary>
public class SolutionFile
{
  private readonly List<ProjectFile2> _projectFiles;

  /// <summary>
  /// Create a new SolutionFile
  /// </summary>
  /// <param name="fileName">
  /// The path to a solution file
  /// </param>
  /// <param name="virtualPathDb">
  /// An optional <see cref="VirtualPathDb"/> instance. If given, paths in this object are
  /// mapped to virtual paths.
  /// </param>
  public SolutionFile(
    string fileName,
    VirtualPathDb? virtualPathDb)
  {
    _projectFiles = [];
    VpDb = virtualPathDb;
    RecognizedProjects = _projectFiles.AsReadOnly();
    FullName = Path.GetFullPath(fileName);
    if(virtualPathDb == null)
    {
      Prefix = String.Empty;
      VPath = null;
    }
    else
    {
      VPath = virtualPathDb.MatchPath(FullName);
      if(VPath == null)
      {
        throw new InvalidOperationException(
          $"Solution file is not in any defined virtual path: {FullName}");
      }
      else
      {
        var def = virtualPathDb.GetDefinition(VPath);
        Prefix = def.Prefix;
      }
    }
    if(FullName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
    {
      UiFullName = FullName.Substring(Prefix.Length);
    }
    else
    {
      UiFullName = FullName;
    }
    SolutionName =
      Path.GetFileNameWithoutExtension(FullName).Replace(' ', '-');
  }

  ///// <summary>
  ///// JSON constructor
  ///// </summary>
  ///// <param name="id"></param>
  ///// <param name="fullpath"></param>
  ///// <param name="uiname"></param>
  ///// <param name="prefix"></param>
  ///// <param name="index">
  ///// Ignored: <see cref="Index"/> is initialized from <paramref name="id"/>,
  ///// but passing it here prevents the deserializer from using the setter.
  ///// </param>
  //[JsonConstructor]
  //public SolutionFile(
  //  string id,
  //  string fullpath,
  //  string uiname,
  //  string prefix = "",
  //  int index = 0) // not actually used; Index is initialized from "id"
  //{
  //  _projectFiles = [];
  //  RecognizedProjects = _projectFiles.AsReadOnly();
  //  var idparts = id.Split('#');
  //  if(idparts.Length == 2)
  //  {
  //    SolutionName = idparts[0];
  //    Index = Int32.Parse(idparts[1]);
  //  }
  //  else
  //  {
  //    SolutionName = id;
  //    Index = 0;
  //  }
  //  FullName = fullpath;
  //  UiFullName = uiname;
  //  Prefix = prefix;
  //}

  /// <summary>
  /// An identifier, generated from <see cref="SolutionName"/> and
  /// <see cref="Index"/>
  /// </summary>
  [JsonProperty("id")]
  public string Id =>
    Index == 0 ? SolutionName : $"{SolutionName}#{Index}";

  /// <summary>
  /// The full path to the solution file
  /// </summary>
  [JsonProperty("fullpath")]
  public string FullName { get; }

  /// <summary>
  /// Only serialize <see cref="FullName"/> if there is no <see cref="VPath"/>.
  /// </summary>
  /// <returns></returns>
  public bool ShouldSerializeFullName()
  {
    return VPath == null;
  }

  /// <summary>
  /// The virtual path to the solution file, if defined
  /// </summary>
  [JsonProperty("vpath")]
  public VirtualPath? VPath { get; }

  /// <summary>
  /// The solution file name with path and extension removed and spaces
  /// replaced:
  /// the "short" solution name
  /// </summary>
  [JsonProperty("name")]
  public string SolutionName { get; }

  /// <summary>
  /// The "full" name of the solution file for UI purposes:
  /// <see cref="FullName"/> with <see cref="Prefix"/> removed if
  /// it starts with <see cref="Prefix"/>, or <see cref="FullName"/>
  /// otherwise.
  /// </summary>
  [JsonProperty("uiname")]
  public string UiFullName { get; }

  /// <summary>
  /// The prefix to <see cref="FullName"/> to leave out when showing
  /// the "full" path. An empty string by default.
  /// </summary>
  [JsonProperty("prefix")]
  public string Prefix { get; }

  /// <summary>
  /// Only serialize prefix if there is no virtual path
  /// </summary>
  /// <returns></returns>
  public bool ShouldSerializePrefix()
  {
    return VPath == null;
  }

  /// <summary>
  /// An index to disambiguate conflicting names
  /// </summary>
  [JsonProperty("index")]
  public int Index { get; set; }

  /// <summary>
  /// The virtual path database in use, if any
  /// </summary>
  [JsonIgnore]
  public VirtualPathDb? VpDb { get; }

  /// <summary>
  /// Controls whether to serialize the <see cref="Index"/> field
  /// </summary>
  public bool ShouldSerializeIndex()
  {
    return Index != 0;
  }

  /// <summary>
  /// The visual studio version associated with the solution
  /// </summary>
  [JsonProperty("vsversion")]
  public string? VisualStudioVersion => Content?.VisualStudioVersion;

  /// <summary>
  /// Determines whether to serialize <see cref="VisualStudioVersion"/>
  /// </summary>
  /// <returns></returns>
  public bool ShouldSerializeVisualStudioVersion()
  {
    return VisualStudioVersion != null && VisualStudioVersion != "0.0.0.0";
  }

  /// <summary>
  /// The number of projects loaded (0 before calling
  /// <see cref="Load"/>)
  /// </summary>
  [JsonProperty("projectcount")]
  public int ProjectCount => Content?.Projects.Count ?? 0;

  /// <summary>
  /// The number of analyzable projects loaded (0 before calling
  /// <see cref="Load"/>)
  /// </summary>
  [JsonProperty("recognizedprojectcount")]
  public int SupportedProjectCount =>
    Content?.Projects.Count(p => p.CanAnalyze) ?? 0;

  /// <summary>
  /// Project information for the projects of recognized types in this
  /// solution. Empty before calling <see cref="Load"/> and
  /// <see cref="LinkProjects"/>.
  /// </summary>
  [JsonProperty("recognizedprojects")]
  public IReadOnlyList<ProjectFile2> RecognizedProjects { get; }

  /// <summary>
  /// True if there are any supported projects at all
  /// </summary>
  [JsonIgnore]
  public bool HasSupportedProjects =>
    Content?.Projects.Any(p => p.CanAnalyze) ?? false;

  /// <summary>
  /// Load the solution content (<see cref="Content"/>) if not done so yet.
  /// </summary>
  /// <param name="reload">
  /// If true, reload even if already loaded
  /// </param>
  public void Load(bool reload = false)
  {
    if(reload || Content == null)
    {
      Content = SolutionInfo.FromFile(FullName);
    }
  }

  /// <summary>
  /// The raw solution content (after calling <see cref="Load"/>)
  /// </summary>
  [JsonIgnore]
  public SolutionInfo? Content { get; private set; }

  /// <summary>
  /// Fill <see cref="RecognizedProjects"/>.
  /// </summary>
  public void LinkProjects()
  {
    if(Content == null)
    {
      throw new InvalidOperationException(
        "Expecting a call to Load() first");
    }
    _projectFiles.Clear();
    foreach(var spi in Content.Projects)
    {
      var pf2 = ProjectFile2.TryCreate(this, spi);
      if(pf2 != null)
      {
        _projectFiles.Add(pf2);
      }
    }
  }
}
