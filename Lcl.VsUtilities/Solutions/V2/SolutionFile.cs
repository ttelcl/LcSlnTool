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
/// Reference to a solution file
/// </summary>
public class SolutionFile
{
  /// <summary>
  /// Create a new SolutionFile
  /// </summary>
  /// <param name="fileName">
  /// The path to a solution file
  /// </param>
  /// <param name="prefix">
  /// Optional: a prefix (root folder) to the full solution file path that will be
  /// stripped off in UI when describing the "full" name of the file
  /// </param>
  public SolutionFile(
    string fileName,
    string? prefix = null)
  {
    FullName = Path.GetFullPath(fileName);
    if(String.IsNullOrEmpty(prefix))
    {
      Prefix = String.Empty;
    }
    else
    {
      prefix = Path.GetFullPath(prefix);
      if(!prefix.EndsWith(Path.DirectorySeparatorChar))
      {
        prefix += Path.DirectorySeparatorChar;
      }
      Prefix = prefix;
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

  /// <summary>
  /// JSON constructor
  /// </summary>
  /// <param name="id"></param>
  /// <param name="fullpath"></param>
  /// <param name="uiname"></param>
  /// <param name="prefix"></param>
  /// <param name="index">
  /// Ignored: <see cref="Index"/> is initialized from <paramref name="id"/>,
  /// but passing it here prevents the deserializer from using the setter.
  /// </param>
  [JsonConstructor]
  public SolutionFile(
    string id,
    string fullpath,
    string uiname,
    string prefix = "",
    int index = 0) // not actually used; Index is initialized from "id"
  {
    var idparts = id.Split('#');
    if(idparts.Length == 2)
    {
      SolutionName = idparts[0];
      Index = Int32.Parse(idparts[1]);
    }
    else
    {
      SolutionName = id;
      Index = 0;
    }
    FullName = fullpath;
    UiFullName = uiname;
    Prefix = prefix;
  }

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
  /// An index to disambiguate conflicting names
  /// </summary>
  [JsonProperty("index")]
  public int Index { get; set; }

  /// <summary>
  /// Controls whether to serialize the <see cref="Index"/> field
  /// </summary>
  public bool ShouldSerializeIndex()
  {
    return Index != 0;
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
  [JsonProperty("recognizedprojects")]
  public int SupportedProjectCount =>
    Content?.Projects.Count(p => p.CanAnalyze) ?? 0;

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
}
