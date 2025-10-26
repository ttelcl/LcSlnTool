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
/// A reference from one project to another
/// </summary>
public class ProjectRef
{
  /// <summary>
  /// Create a new ProjectRef
  /// </summary>
  public ProjectRef(
    string source,
    string target,
    [JsonProperty("type")] ReferenceType refType,
    [JsonProperty("intern")] bool isInternal = false)
  {
    SourceProject = source;
    TargetProject = target;
    RefType = refType;
    IsInternal = isInternal;
  }

  /// <summary>
  /// The name of the source project (that references <see cref="TargetProject"/>)
  /// </summary>
  [JsonProperty("source")]
  public string SourceProject { get; }

  /// <summary>
  /// The name of the target project (referenced by <see cref="SourceProject"/>).
  /// For now we ignore versions.
  /// </summary>
  [JsonProperty("target")]
  public string TargetProject { get; }

  /// <summary>
  /// The type of reference
  /// </summary>
  [JsonProperty("type")]
  public ReferenceType RefType { get; }

  /// <summary>
  /// Whether the target project is part of the same scan. Mutable.
  /// Initially false.
  /// </summary>
  [JsonProperty("intern")]
  public bool IsInternal { get; set; }
}
