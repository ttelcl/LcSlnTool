﻿/*
 * (c) 2017 ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lcl.VsUtilities.Solutions;

/// <summary>
/// Stores all information about a solution and its projects
/// </summary>
public class Solution
{
  private List<ProjectDetails> _projectList;
  //private Dictionary<Guid, ProjectDetails> _projectMap;

  /// <summary>
  /// Create a new Solution from a pre-loaded SolutionInfo,
  /// loading the projects
  /// </summary>
  public Solution(SolutionInfo si)
  {
    Info = si;
    _projectList = new List<ProjectDetails>();
    //_projectMap = new Dictionary<Guid, ProjectDetails>();
    Projects = _projectList.AsReadOnly();
    foreach(var spi in Info.Projects)
    {
      var details = Info.LoadProject(spi, true)!;
      _projectList.Add(details);
      //_projectMap[details.Id] = details;
    }

  }

  /// <summary>
  /// Load a solution and its projects
  /// </summary>
  public Solution(string solutionFileName)
    : this(SolutionInfo.FromFile(solutionFileName))
  {
  }

  /// <summary>
  /// The basic solution info obtained from the solution file
  /// </summary>
  public SolutionInfo Info { get; }

  /// <summary>
  /// The basic project info list, loaded from the solution file
  /// </summary>
  public IReadOnlyList<SolutionProjectInfo> ProjectInfos { get { return Info.Projects; } }

  /// <summary>
  /// The full project info list, loaded from solution file and project files
  /// </summary>
  public IReadOnlyList<ProjectDetails> Projects { get; }

  /// <summary>
  /// Build the JSON serializable project summaries and collect them in a map
  /// </summary>
  /// <param name="graph"></param>
  /// <returns></returns>
  public IReadOnlyDictionary<string, ProjectSummary> BuildProjectSummaries(
    ProjectDependencyGraph graph)
  {
    var map = new Dictionary<Guid, ProjectSummary>();
    var sortguide = graph.TopologicallySorted;
    foreach(var project in Projects)
    {
      var summary = ProjectSummary.FromProject(project, graph);
      if(summary != null)
      {
        map.Add(summary.Id,  summary);
      }
    }
    var result =
      sortguide
      .Where(n => map.ContainsKey(n.ProjectId))
      .Select(n => map[n.ProjectId])
      .ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);
    return result;
  }

}

