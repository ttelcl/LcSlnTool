/*
 * (c) 2017 ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lcl.VsUtilities.Solutions;

/// <summary>
/// A reference to a project (from another project)
/// </summary>
public class ProjectReference
{
  /// <summary>
  /// Create a new ProjectReference
  /// </summary>
  public ProjectReference(string name, /*Guid projectId,*/ string include)
  {
    Name = name;
    // ProjectId = projectId;
    Include = include ?? String.Empty;
  }

  /// <summary>
  /// Name of the referenced project
  /// </summary>
  public string Name { get; }

  ///// <summary>
  ///// Id of the referenced project
  ///// </summary>
  //public Guid ProjectId { get; }

  /// <summary>
  /// The "Include" attribute, or an empty string if not available
  /// </summary>
  public string Include { get; }

}

