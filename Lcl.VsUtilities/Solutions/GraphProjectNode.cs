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
/// A node in the project dependency graph
/// </summary>
public class GraphProjectNode
{
  private List<GraphProjectNode> _dependsOn;
  private List<GraphProjectNode> _dependentOf;

  internal GraphProjectNode(ProjectDependencyGraph owner, ProjectDetails project)
  {
    Owner = owner;
    Project = project;
    _dependsOn = new List<GraphProjectNode>();
    _dependentOf = new List<GraphProjectNode>();
    DependsOn = _dependsOn.AsReadOnly();
    DependentOf = _dependentOf.AsReadOnly();
  }

  /// <summary>
  /// The graph this is part of
  /// </summary>
  public ProjectDependencyGraph Owner { get; }

  /// <summary>
  /// The project this node describes
  /// </summary>
  public ProjectDetails Project { get; }

  /// <summary>
  /// The list of nodes this project depends on
  /// </summary>
  public IReadOnlyList<GraphProjectNode> DependsOn { get; }

  /// <summary>
  /// The list of nodes that depend on this project
  /// </summary>
  public IReadOnlyList<GraphProjectNode> DependentOf { get; }

  /// <summary>
  /// True if this is a leaf node, not depending on any other nodes
  /// </summary>
  public bool IsLeaf { get { return DependsOn.Count == 0; } }

  /// <summary>
  /// True if this is a root node, that no other nodes depend on
  /// </summary>
  public bool IsRoot { get { return DependentOf.Count == 0; } }

  /// <summary>
  /// Get the project label
  /// </summary>
  public string Label { get { return Project.Label; } }

  /// <summary>
  /// True if this is a node of a stub project (such as a solution folder)
  /// that is not likely to be truly part of the dependency graph
  /// </summary>
  public bool IsStub { get { return Project.IsStub; } }

  /// <summary>
  /// The sort order in the topological sort. Set by 
  /// <see cref="ProjectDependencyGraph.TopologicallySort()"/>
  /// </summary>
  public int TopoSortOrder { get; internal set; } = -1;

  /// <summary>
  /// Look up the value for this node in the specified map, returning the
  /// specified default value if not found
  /// </summary>
  public T Lookup<T>(Dictionary<string, T> map, T defaultValue)
  {
    return map.TryGetValue(Label, out var t) ? t : defaultValue;
  }

  /// <summary>
  /// Look up the value for this node in the specified map, throwing 
  /// an exception if not found
  /// </summary>
  public T Lookup<T>(Dictionary<string, T> map)
  {
    return map[Label];
  }

  /// <summary>
  /// Set a value for the associated entry in the map
  /// </summary>
  public void Set<T>(Dictionary<string, T> map, T value)
  {
    map[Label] = value;
  }

  internal void RegisterDependenceOn(GraphProjectNode target)
  {
    _dependsOn.Add(target);
  }

  internal void RegisterDependentOf(GraphProjectNode source)
  {
    _dependentOf.Add(source);
  }

}

