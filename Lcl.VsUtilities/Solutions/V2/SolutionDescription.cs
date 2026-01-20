/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Lcl.VsUtilities.Solutions.V2;

/// <summary>
/// A serializable wrapper around <see cref="SolutionInfo"/>
/// </summary>
public class SolutionDescription
{

  /// <summary>
  /// Create a new SolutionDescription
  /// </summary>
  public SolutionDescription(
    SolutionInfo raw)
  {
    Raw = raw;
  }

  /// <summary>
  /// The wrapped raw solution info
  /// </summary>
  [JsonIgnore]
  public SolutionInfo Raw { get; }

  /// <summary>
  /// The solution name
  /// </summary>
  [JsonProperty("name")]
  public string Name => Raw.Name;

  /// <summary>
  /// The solution file name
  /// </summary>
  [JsonProperty("filename")]
  public string SolutionFile => Raw.SolutionFile;

  /// <summary>
  /// Visual Studio version (if known; "0.0.0.0" otherwise)
  /// </summary>
  public string VsVersion => Raw.VisualStudioVersion;
}
