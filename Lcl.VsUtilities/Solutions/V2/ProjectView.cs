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
/// A view on projects and solutions and their relations, from
/// a project-centric viewpoint.
/// </summary>
public class ProjectView
{
  /// <summary>
  /// Create a new ProjectView
  /// </summary>
  public ProjectView(
    IDictionary<string, IReadOnlyList<ProjectFile3>> projects)
  {
    ProjectMap = new Dictionary<string, IReadOnlyList<ProjectFile3>>(
      projects,
      StringComparer.OrdinalIgnoreCase);
    ProjectNames = new HashSet<string>(ProjectMap.Keys, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Convert a <see cref="SolutionView"/> to a <see cref="ProjectView"/>
  /// </summary>
  /// <param name="solutionView"></param>
  /// <returns></returns>
  public static ProjectView FromSolutionView(SolutionView solutionView)
  {
    var solutions = solutionView.Solutions.ToList();
    var solutions3 = new Dictionary<string, SolutionInfo3>(
      StringComparer.OrdinalIgnoreCase);
    foreach(var sf in solutions)
    {
      var si3 = SolutionInfo3.FromSolutionFile(sf);
      if(solutions3.ContainsKey(si3.Id))
      {
        throw new InvalidOperationException(
          $"Expecting solution IDs to be case-insensitively unique, but found a second '{si3.Id}'");
      }
      solutions3[si3.Id] = si3;
    }
    var projects = solutionView.EnumProjectsSorted().ToList();
    var projectsByName =
      from project in projects
      group project by project.Name.ToLowerInvariant();
    var projects3 = new Dictionary<string, IReadOnlyList<ProjectFile3>>(
      StringComparer.OrdinalIgnoreCase);
    foreach(var projectByName in projectsByName)
    {
      var pf3list = new List<ProjectFile3>();
      var projectsByNameAndFile =
        from pf2 in projectByName
        group pf2 by pf2.VPath.VPath.ToLowerInvariant();
      foreach(var projectByNameAndFile in projectsByNameAndFile)
      {
        var p2list = projectByNameAndFile.ToList();
        var p2sample = p2list[0];
        // Normally expected to execute once per project, but may happen more
        // often in case projects are duplicated.
        // Assumption: the projects in projectByNameAndFile only differ in SolutionId
        var solutionIds =
          from pf2 in p2list
          orderby pf2.SolutionId.ToLowerInvariant()
          select pf2.SolutionId;
        var pf3 = new ProjectFile3(
          p2sample.FullPath,
          p2sample.Name,
          solutionIds.Select(sid => solutions3[sid]),
          p2sample.VPath);
        pf3list.Add(pf3);
      }
      projects3.Add(pf3list[0].Name, pf3list);
    }
    return new ProjectView(projects3);
  }

  /// <summary>
  /// A (case-insensitive) mapping of project names to one or more
  /// project files providing that project. In the ideal case each 
  /// project name has exactly 1 providing project.
  /// </summary>
  [JsonProperty("projects")]
  public IReadOnlyDictionary<string, IReadOnlyList<ProjectFile3>> ProjectMap { get; }

  /// <summary>
  /// The set of project names in this view
  /// </summary>
  [JsonIgnore]
  public IReadOnlySet<string> ProjectNames { get; }

  /// <summary>
  /// Parse all project files and return a sequence of references found
  /// in them.
  /// </summary>
  /// <returns></returns>
  public IEnumerable<ProjectRef> ParseReferences()
  {
    var projects = ProjectMap.Values.SelectMany(l => l);
    return projects.SelectMany(pf3 => pf3.ParseReferences(ProjectNames));
  }

  /// <summary>
  /// A simplistic *.dot file writer
  /// </summary>
  /// <param name="fileName">
  /// The name of the file to write
  /// </param>
  /// <param name="references">
  /// The references to take into account
  /// </param>
  /// <param name="horizontal"></param>
  public void WriteSimpleDot(
    string fileName,
    IEnumerable<ProjectRef> references,
    bool horizontal = false)
  {
    using var dotWriter = new DotFileWriter(
      fileName,
      true,
      horizontal,
      null);
    // names of referenced projects (nodes)
    var projects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    // the references we can use
    var approvedRefs = new List<ProjectRef>();
    foreach(var reference in references)
    {
      var src = reference.SourceProject;
      var tgt = reference.TargetProject;
      if(ProjectNames.Contains(src) && ProjectNames.Contains(tgt))
      {
        approvedRefs.Add(reference);
        projects.Add(src);
        projects.Add(tgt);
      }
    }
    foreach(var project in projects)
    {
      dotWriter.AddNode(
        project, []);
    }
    foreach(var reference in approvedRefs)
    {
      dotWriter.AddEdge(
        reference.SourceProject, reference.TargetProject, false, null);
    }
  }
}
