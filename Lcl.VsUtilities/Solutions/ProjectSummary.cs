/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Lcl.VsUtilities.Solutions;

/// <summary>
/// Description of ProjectSummary
/// </summary>
public class ProjectSummary
{
  /// <summary>
  /// Create a new ProjectSummary
  /// </summary>
  public ProjectSummary(
    string name,
    string treePath,
    string projectPath,
    Guid id,
    string? sdk,
    IEnumerable<string> frameworks,
    IEnumerable<string> directrefs,
    IEnumerable<string> allrefs,
    int sortindex = -1)
  {
    Name = name;
    TreePath = treePath;
    ProjectPath = projectPath;
    Id = id;
    Sdk = sdk;
    Frameworks = frameworks.ToList().AsReadOnly();
    DirectRefs = directrefs.ToList();
    AllRefs = allrefs.ToList();
    SortIndex = sortindex;
  }

  /// <summary>
  /// Create a summary from project details
  /// </summary>
  public static ProjectSummary? FromProject(
    ProjectDetails project,
    ProjectDependencyGraph graph)
  {
    if(project.IsStub)
    {
      return null;
    }
    var prf = project.Content!;
    var name = project.Label;
    var projectId = project.ProjectId;
    var treePath = project.Meta.SolutionTreePath();
    var id = project.Meta.Id;
    var directDependencies =
      project
      .ProjectReferences
      .Select(r => r.Name)
      .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var pNode = graph.FindNodeById(projectId);
    if(pNode == null)
    {
      throw new InvalidOperationException(
        $"Internal error: project '{projectId}' not found in dependency graph");
    }
    var deepDependencies =
      graph
      .GetDeepDependsOn(pNode)
      .Select(n => n.Label)
      .ToHashSet(StringComparer.OrdinalIgnoreCase);
    return new ProjectSummary(
      name,
      treePath,
      project.Meta.Path,
      id,
      prf.Sdk,
      prf.Frameworks,
      directDependencies,
      deepDependencies,
      pNode.TopoSortOrder);
  }

  /// <summary>
  /// The name of the project
  /// </summary>
  [JsonProperty("name")]
  public string Name { get; }

  /// <summary>
  /// The sort order in a topological sort. All nodes this node
  /// depends on will have a lower value.
  /// Set to -1 if not calculated.
  /// </summary>
  [JsonProperty("sortindex")]
  [DefaultValue(-1)]
  public int SortIndex { get; } = -1;

  /// <summary>
  /// Whether or not to serialize the <see cref="SortIndex"/> field
  /// </summary>
  public bool ShouldSerializeSortIndex()
  {
    return SortIndex != -1;
  }

  /// <summary>
  /// The path of this project in the solution tree
  /// </summary>
  [JsonProperty("treepath")]
  public string TreePath { get; }

  /// <summary>
  /// The path to the project file
  /// </summary>
  [JsonProperty("projectpath")]
  public string ProjectPath { get; }

  /// <summary>
  /// The project's GUID in the solution. This concept may be removed in the future!
  /// </summary>
  [JsonProperty("id")]
  public Guid Id { get; }

  /// <summary>
  /// The names of projects this one directly depends on
  /// </summary>
  [JsonProperty("directrefs")]
  public IReadOnlyList<string> DirectRefs { get; }

  /// <summary>
  /// The names of projects this one depends on directly or indirectly
  /// </summary>
  [JsonProperty("allrefs")]
  public IReadOnlyList<string> AllRefs { get; }

  /// <summary>
  /// The SDK used by this project, if known
  /// </summary>
  [JsonProperty("sdk")]
  public string? Sdk { get; }

  /// <summary>
  /// The target frameworks, if known
  /// </summary>
  [JsonProperty("frameworks")]
  public IReadOnlyList<string> Frameworks { get; }
}
