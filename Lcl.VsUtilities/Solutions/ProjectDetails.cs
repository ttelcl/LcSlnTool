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
/// Combines a SolutionProjectInfo and a ProjectFile
/// </summary>
public class ProjectDetails
{
  /// <summary>
  /// Create a new ProjectDetails
  /// </summary>
  public ProjectDetails(SolutionProjectInfo spi, ProjectFile? prjf=null)
  {
    Meta = spi;
    Content = prjf ?? new ProjectFile([], null);
    IsStub = prjf == null;
  }

  /// <summary>
  /// Project information from the solution file
  /// </summary>
  public SolutionProjectInfo Meta { get; }

  /// <summary>
  /// Project information from the project file (possibly an empty stub)
  /// </summary>
  public ProjectFile Content { get; }

  /// <summary>
  /// True if the Content is an empty stub because there was no projectfile
  /// </summary>
  public bool IsStub { get; }

  /// <summary>
  /// Identifies the type of project (forwarded from Meta)
  /// </summary>
  public Guid ProjectTypeId { get { return Meta.ProjectTypeId; } }

  /// <summary>
  /// Get a name for the project type
  /// </summary>
  public string ProjectTypeName { get { return ProjectTypes.ProjectTypeName(ProjectTypeId); } }

  /// <summary>
  /// Identifies the project (forwarded from Meta)
  /// </summary>
  public Guid Id { get { return Meta.Id; } }

  /// <summary>
  /// Name for the project (forwarded from Meta)
  /// </summary>
  public string Label { get { return Meta.Label; } }

  /// <summary>
  /// The project references (forwarded from Content)
  /// </summary>
  public IReadOnlyList<ProjectReference> ProjectReferences { get { return Content.ProjectReferences; } }

}

