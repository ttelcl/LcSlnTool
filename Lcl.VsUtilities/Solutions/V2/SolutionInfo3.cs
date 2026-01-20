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
/// A brief description of a solution file, for use in a 
/// project-oriented view
/// </summary>
public class SolutionInfo3
{
  /// <summary>
  /// Create a new SolutionInfo3
  /// </summary>
  public SolutionInfo3(
    string fullpath,
    string id)
  {
    FullPath = fullpath;
    Id = id;
  }

  /// <summary>
  /// Instantiate a <see cref="SolutionInfo3"/> based on a
  /// <see cref="SolutionFile"/>.
  /// </summary>
  /// <param name="sf"></param>
  /// <returns></returns>
  public static SolutionInfo3 FromSolutionFile(SolutionFile sf)
  {
    return new SolutionInfo3(sf.FullName, sf.Id);
  }

  /// <summary>
  /// The solution Id
  /// </summary>
  [JsonProperty("id")]
  public string Id { get; }

  /// <summary>
  /// The full path to the solution file
  /// </summary>
  [JsonProperty("fullpath")]
  public string FullPath { get; }

}
