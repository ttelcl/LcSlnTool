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
    IReadOnlyDictionary<string, bool> references)
  {
    Name = name;
    TreePath = treePath;
    ProjectPath = projectPath;
    Id = id;
    References = references.ToDictionary(
      kvp => kvp.Key,
      kvp => kvp.Value,
      StringComparer.OrdinalIgnoreCase);
  }

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
    var treePath = project.Meta.SolutionTreePath();
    var id = project.Meta.Id;
    var directDependencies =
      project
      .ProjectReferences
      .Select(r => r.Name)
      .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var pNode = graph.FindNodeById(name);
    if(pNode == null)
    {
      throw new InvalidOperationException(
        "Internal error: project not found in dependency graph");
    }
    var deepDependencies =
      graph
      .GetDeepDependsOn(pNode)
      .Select(n => n.Label)
      .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var references = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    foreach(var deepDep in deepDependencies)
    {
      references[deepDep] = false;
    }
    foreach(var directDep in directDependencies)
    {
      references[directDep] = true;
    }
    return new ProjectSummary(
      name,
      treePath,
      project.Meta.Path,
      id,
      references);
  }

  /// <summary>
  /// The name of the project
  /// </summary>
  [JsonProperty("name")]
  public string Name { get; }

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
  /// The names of projects this one depends on; with value 'true' for
  /// direct references and 'false' for indirect ones.
  /// </summary>
  [JsonProperty("references")]
  public IReadOnlyDictionary<string, bool> References { get; }
}
