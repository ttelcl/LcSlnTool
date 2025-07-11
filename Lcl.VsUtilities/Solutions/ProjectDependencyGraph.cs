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
/// Maps dependencies between projects in a solution
/// </summary>
public class ProjectDependencyGraph
{
  private Dictionary<Guid, GraphProjectNode> _nodes;
  private List<ProjectReference> _missingTargets;
  private Dictionary<Guid, HashSet<Guid>>? _deepDependsOnCache = null;
  private Dictionary<Guid, HashSet<Guid>>? _deepDependentOfCache = null;
  private IReadOnlyList<GraphProjectNode>? _topoSortList = null;

  /// <summary>
  /// Create a new ProjectDependencyGraph
  /// </summary>
  public ProjectDependencyGraph(Solution sln)
  {
    Solution = sln;
    _missingTargets = new List<ProjectReference>();
    _nodes = new Dictionary<Guid, GraphProjectNode>();
    _deepDependsOnCache = null;
    _deepDependentOfCache = null;
    _topoSortList = null;

    // initialize nodes (without initializing references)
    foreach(var prj in Solution.Projects)
    {
      var node = new GraphProjectNode(this, prj);
      if(_nodes.TryGetValue(node.ProjectId, out var existing))
      {
        throw new InvalidOperationException(
          $"Project name conflict: {node.Project.Meta.Path} vs {existing.Project.Meta.Path}");
      }
      _nodes[node.ProjectId] = node;
    }

    // initialize dependencies
    foreach(var depender in _nodes.Values)
    {
      foreach(var reference in depender.Project.ProjectReferences)
      {
        if(_nodes.TryGetValue(reference.ProjectId, out var node))
        {
          RegisterDependence(depender, node);
        }
        else
        {
          _missingTargets.Add(reference);
        }
      }
    }
  }

  /// <summary>
  /// The solution this graph applies to
  /// </summary>
  public Solution Solution { get; }

  /// <summary>
  /// Enumerate the project nodes
  /// </summary>
  public IEnumerable<GraphProjectNode> Nodes { get { return _nodes.Values; } }

  /// <summary>
  /// Remove nodes that are not referenced nor have references that started
  /// as stub nodes (i.e. no project information was available). Returns
  /// a list of the removed nodes.
  /// </summary>
  public IReadOnlyList<GraphProjectNode> StripSingletonStubs()
  {
    var list = new List<GraphProjectNode>();
    foreach(var node in _nodes.Values)
    {
      if(node.IsLeaf && node.IsRoot && node.IsStub)
      {
        list.Add(node); // cannot delete during iteration, so add to list instead
      }
    }
    foreach(var node in list)
    {
      _nodes.Remove(node.ProjectId);
    }
    _deepDependentOfCache = null;
    _deepDependsOnCache = null;
    _topoSortList = null;
    return list.AsReadOnly();
  }

  /// <summary>
  /// Find a project node by Name, returning null if not found
  /// </summary>
  public GraphProjectNode? FindNodeById(Guid projectId)
  {
    return _nodes.TryGetValue(projectId, out var node) ? node : null;
  }

  /// <summary>
  /// Calculate the leaf levels for all nodes, returning them as a dictionary
  /// mapping Project GUIDs to the leaf level
  /// </summary>
  public Dictionary<Guid, int> GetLeafLevels()
  {
    var map = new Dictionary<Guid, int>();
    foreach(var p in Nodes)
    {
      var unused = GetNodeLevelFor(map, p, n => n.DependsOn, 32);
    }
    return map;
  }

  /// <summary>
  /// Calculate the root levels for all nodes, returning them as a dictionary
  /// mapping Project GUIDs to the leaf level
  /// </summary>
  public Dictionary<Guid, int> GetRootLevels()
  {
    var map = new Dictionary<Guid, int>();
    foreach(var p in Nodes)
    {
      var unused = GetNodeLevelFor(map, p, n => n.DependentOf, 32);
    }
    return map;
  }

  /// <summary>
  /// Find the ids of projects a project depends on, that none of its
  /// recursive dependencies themselves depend on
  /// </summary>
  public HashSet<Guid> FindPureDependencies(GraphProjectNode node)
  {
    if(_deepDependsOnCache == null)
    {
      _deepDependsOnCache = new Dictionary<Guid, HashSet<Guid>>();
    }
    // initialize _deepDependsOnCache for all relevant nodes
    var deepDepIds = _GetDeepChildIds(_deepDependsOnCache, node, n => n.DependsOn, 32);
    var result = new HashSet<Guid>(deepDepIds);
    foreach(var child in node.DependsOn)
    {
      var deepChildIds = _deepDependsOnCache[child.ProjectId];
      result.ExceptWith(deepChildIds);
    }
    return result;
  }

  /// <summary>
  /// Calculate the list of all nodes the specified node depends on
  /// (directly or indirectly)
  /// </summary>
  public IReadOnlyList<GraphProjectNode> GetDeepDependsOn(GraphProjectNode node)
  {
    if(_deepDependsOnCache == null)
    {
      _deepDependsOnCache = new Dictionary<Guid, HashSet<Guid>>();
    }
    var idSet = _GetDeepChildIds(_deepDependsOnCache, node, n => n.DependsOn, 32);
    var list = new List<GraphProjectNode>(
      from id in idSet
      let n = FindNodeById(id)
      // orderby n.Project.Label
      select n);
    return list.AsReadOnly();
  }

  /// <summary>
  /// Calculate the list of all nodes that depend on the specified node
  /// (directly or indirectly)
  /// </summary>
  public IReadOnlyList<GraphProjectNode> GetDeepDependentOf(GraphProjectNode node)
  {
    if(_deepDependentOfCache == null)
    {
      _deepDependentOfCache = new Dictionary<Guid, HashSet<Guid>>();
    }
    var idSet = _GetDeepChildIds(_deepDependentOfCache, node, n => n.DependentOf, 32);
    var list = new List<GraphProjectNode>(
      from id in idSet
      let n = FindNodeById(id)
      // orderby n.Project.Label
      select n);
    return list.AsReadOnly();
  }

  /// <summary>
  /// Save a GraphViz *.dot file depicting projects and dependencies,
  /// from leaf nodes to root nodes
  /// </summary>
  /// <param name="dotFileName">
  /// The name of the file to save
  /// </param>
  /// <param name="pureOnly">
  /// Leave out redundant edges if true
  /// </param>
  /// <param name="vertical">
  /// If true: use a vertical layout
  /// </param>
  public void SaveDotFile(
    string dotFileName,
    bool pureOnly,
    bool vertical)
  {
    using var dot = new DotFileWriter(
      dotFileName,
      true,
      !vertical,
      Solution.Info.Name);
    var nodes = TopologicallySorted.ToList();
    nodes.Reverse();
    foreach(var node in Nodes)
    {
      var color =
        node.IsRoot
        ? node.IsLeaf ? "#FF9977" : "#FFFF77"
        : node.IsLeaf ? "#99FF99" : "#DDDDDD";
      dot.AddNode(
        node.Label,
        node.Project.Content.Frameworks,
        shape: "box",
        style: "filled",
        color: color,
        id: node.Label);
    }
    foreach(var node in Nodes)
    {
      var pureIds = FindPureDependencies(node);
      var nodeFrameworks =
        node.Project.Content.Frameworks.ToHashSet(
          StringComparer.OrdinalIgnoreCase);
      foreach(var dnode in node.DependsOn)
      {
        var dnodeFrameworks =
          dnode.Project.Content.Frameworks.ToHashSet(
            StringComparer.OrdinalIgnoreCase);
        var sameFrameworks = nodeFrameworks.SetEquals(dnodeFrameworks);
        var isPure = pureIds.Contains(dnode.ProjectId);
        if(pureOnly)
        {
          if(isPure) // else: ignore edge
          {
            var color = sameFrameworks ? "#228822" : "#DD5522";
            dot.AddEdge(
              node.Label,
              dnode.Label,
              weightless: false,
              color: color);
          }
        }
        else
        {
          var color =
            isPure ? sameFrameworks ? "#228822" : "#DD5522" : "#999999";
          dot.AddEdge(
            node.Label,
            dnode.Label,
            weightless: !isPure,
            color: color);
        }
      }
    }
  }

  /// <summary>
  /// Return the list of nodes in topologically sorted order, starting
  /// from nodes that have no dependencies. This returns a cached copy
  /// after the first call.
  /// </summary>
  public IReadOnlyList<GraphProjectNode> TopologicallySorted {
    get {
      if(_topoSortList == null)
      {
        _topoSortList = TopologicallySort();
      }
      return _topoSortList;
    }
  }

  /// <summary>
  /// Return the list of nodes in topologically sorted order, starting
  /// from nodes that have no dependencies
  /// </summary>
  private IReadOnlyList<GraphProjectNode> TopologicallySort()
  {
    var result = new List<GraphProjectNode>();
    var freeNodes = new List<GraphProjectNode>();
    var pendinglinks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach(var node in _nodes.Values)
    {
      var count = node.DependsOn.Count;
      node.Set(pendinglinks, count);
      if(count == 0)
      {
        freeNodes.Add(node);
      }
    }
    var candidates = new List<GraphProjectNode>();
    while(freeNodes.Count > 0)
    {
      candidates.Clear();
      foreach(var node in freeNodes)
      {
        result.Add(node);
        foreach(var child in node.DependentOf)
        {
          var childPendingCount = child.Lookup(pendinglinks) - 1;
          child.Set(pendinglinks, childPendingCount);
          if(childPendingCount == 0)
          {
            candidates.Add(child);
          }
        }
      }
      // swap the lists
      var tmp = freeNodes;
      freeNodes = candidates;
      candidates = tmp;
    }
    if(result.Count != _nodes.Count)
    {
      throw new InvalidOperationException(
        $"Dependency graph is not acyclic. {_nodes.Count - result.Count} nodes unaccounted for");
    }
    for(var i = 0; i < result.Count; i++)
    {
      result[i].TopoSortOrder = i;
    }
    return result.AsReadOnly();
  }

  private HashSet<Guid> _GetDeepChildIds(
    Dictionary<Guid, HashSet<Guid>> cache,
    GraphProjectNode node,
    Func<GraphProjectNode, IEnumerable<GraphProjectNode>> getChildren,
    int recursionGuard)
  {
    if(cache.TryGetValue(node.ProjectId, out var result))
    {
      return result;
    }
    if(recursionGuard <= 0)
    {
      throw new InvalidOperationException("Recursion limit exceeded");
    }
    result = new HashSet<Guid>();
    var children = getChildren(node);
    foreach(var child in children)
    {
      if(!result.Contains(child.ProjectId))
      {
        result.Add(child.ProjectId);
        var grandchildIds = _GetDeepChildIds(cache, child, getChildren, recursionGuard - 1);
        result.UnionWith(grandchildIds);
      }
    }
    cache[node.ProjectId] = result;
    return result;
  }

  private int GetNodeLevelFor(
    Dictionary<Guid, int> cache,
    GraphProjectNode node,
    Func<GraphProjectNode, IEnumerable<GraphProjectNode>> getChildren,
    int recursionGuard)
  {
    int level;
    if(cache.TryGetValue(node.ProjectId, out level))
    {
      return level;
    }
    if(recursionGuard<=0)
    {
      throw new InvalidOperationException("Recursion limit exceeded");
    }
    var children = getChildren(node);
    level = 0;
    foreach(var child in children)
    {
      var clvl = 1 + GetNodeLevelFor(cache, child, getChildren, recursionGuard - 1);
      if(clvl>level)
      {
        level = clvl;
      }
    }
    cache[node.ProjectId] = level;
    return level;
  }

  private void RegisterDependence(GraphProjectNode depender, GraphProjectNode dependee)
  {
    depender.RegisterDependenceOn(dependee);
    dependee.RegisterDependentOf(depender);
  }
}

