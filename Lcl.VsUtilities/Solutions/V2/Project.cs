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
/// Project-oriented information
/// </summary>
public class Project
{
  /// <summary>
  /// Create a new Project
  /// </summary>
  public Project(
    string name)
  {
    Name = name;
  }

  /// <summary>
  /// The project name (label)
  /// </summary>
  [JsonProperty("name")]
  public string Name { get; }

}
