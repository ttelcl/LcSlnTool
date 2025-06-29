/*
 * (c) 2017 ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lcl.VsUtilities.Solutions
{
  /// <summary>
  /// Maps dependencies between projects in a solution
  /// </summary>
  public class ProjectDependencyGraph
  {
    private Dictionary<Guid, ProjectNode> _nodes;
    private List<ProjectReference> _missingTargets;
    private Dictionary<Guid, HashSet<Guid>> _deepDependsOnCache = null;
    private Dictionary<Guid, HashSet<Guid>> _deepDependentOfCache = null;

    /// <summary>
    /// Create a new ProjectDependencyGraph
    /// </summary>
    public ProjectDependencyGraph(Solution sln)
    {
      Solution = sln;
      _missingTargets = new List<ProjectReference>();
      _nodes = new Dictionary<Guid, ProjectNode>();
      _deepDependsOnCache = null;
      _deepDependentOfCache = null;

      // initialize nodes (without initializing references)
      foreach (var prj in Solution.Projects)
      {
        var node = new ProjectNode(this, prj);
        _nodes[node.Id] = node;
      }

      // initialize dependencies
      foreach(var depender in _nodes.Values)
      {
        foreach(var reference in depender.Project.ProjectReferences)
        {
          ProjectNode node;
          if(_nodes.TryGetValue(reference.ProjectId, out node))
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
    public IEnumerable<ProjectNode> Nodes { get { return _nodes.Values; } }

    /// <summary>
    /// Remove nodes that are not referenced nor have references that started
    /// as stub nodes (i.e. no project information was available). Returns
    /// a list of the removed nodes.
    /// </summary>
    public IReadOnlyList<ProjectNode> StripSingletonStubs()
    {
      var list = new List<ProjectNode>();
      foreach(var node in _nodes.Values)
      {
        if(node.IsLeaf && node.IsRoot && node.IsStub)
        {
          list.Add(node); // cannot delete during iteration, so add to list instead
        }
      }
      foreach(var node in list)
      {
        _nodes.Remove(node.Id);
      }
      _deepDependentOfCache = null;
      _deepDependsOnCache = null;
      return list.AsReadOnly();
    }

    /// <summary>
    /// Find a project node by Id, returning null if not found
    /// </summary>
    public ProjectNode FindNodeById(Guid id)
    {
      ProjectNode node;
      return _nodes.TryGetValue(id, out node) ? node : null;
    }

    /// <summary>
    /// Calculate the leaf levels for all nodes, returning them as a dictionary
    /// mapping Project GUIDs to the leaf level
    /// </summary>
    public Dictionary<Guid,int> GetLeafLevels()
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
    public Dictionary<Guid,int> GetRootLevels()
    {
      var map = new Dictionary<Guid, int>();
      foreach (var p in Nodes)
      {
        var unused = GetNodeLevelFor(map, p, n => n.DependentOf, 32);
      }
      return map;
    }

    /// <summary>
    /// Find the ids of projects a project depends on that none of its
    /// recursive dependencies themselves depend on
    /// </summary>
    public HashSet<Guid> FindPureDependencies(ProjectNode node)
    {
      if (_deepDependsOnCache == null)
      {
        _deepDependsOnCache = new Dictionary<Guid, HashSet<Guid>>();
      }
      // initialize _deepDependsOnCache for all relevant nodes
      var deepDepIds = _GetDeepChildIds(_deepDependsOnCache, node, n => n.DependsOn, 32);
      var result = new HashSet<Guid>(deepDepIds);
      foreach(var child in node.DependsOn)
      {
        var deepChildIds = _deepDependsOnCache[child.Id];
        result.ExceptWith(deepChildIds);
      }
      return result;
    }

    /// <summary>
    /// Calculate the list of all nodes the specified node depends on
    /// (directly or indirectly)
    /// </summary>
    public IReadOnlyList<ProjectNode> GetDeepDependsOn(ProjectNode node)
    {
      if (_deepDependsOnCache == null)
      {
        _deepDependsOnCache = new Dictionary<Guid, HashSet<Guid>>();
      }
      var idSet = _GetDeepChildIds(_deepDependsOnCache, node, n => n.DependsOn, 32);
      var list = new List<ProjectNode>(
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
    public IReadOnlyList<ProjectNode> GetDeepDependentOf(ProjectNode node)
    {
      if (_deepDependentOfCache == null)
      {
        _deepDependentOfCache = new Dictionary<Guid, HashSet<Guid>>();
      }
      var idSet = _GetDeepChildIds(_deepDependentOfCache, node, n => n.DependentOf, 32);
      var list = new List<ProjectNode>(
        from id in idSet
        let n = FindNodeById(id)
        // orderby n.Project.Label
        select n);
      return list.AsReadOnly();
    }

    private HashSet<Guid> _GetDeepChildIds(
      Dictionary<Guid,HashSet<Guid>> cache,
      ProjectNode node,
      Func<ProjectNode,IEnumerable<ProjectNode>> getChildren,
      int recursionGuard)
    {
      HashSet<Guid> result;
      if(cache.TryGetValue(node.Id, out result))
      {
        return result;
      }
      if (recursionGuard <= 0)
      {
        throw new InvalidOperationException("Recursion limit exceeded");
      }
      result = new HashSet<Guid>();
      var children = getChildren(node);
      foreach(var child in children)
      {
        if(!result.Contains(child.Id))
        {
          result.Add(child.Id);
          var grandchildIds = _GetDeepChildIds(cache, child, getChildren, recursionGuard - 1);
          result.UnionWith(grandchildIds);
        }
      }
      cache[node.Id] = result;
      return result;
    }

    private int GetNodeLevelFor(
      Dictionary<Guid, int> cache,
      ProjectNode node,
      Func<ProjectNode,IEnumerable<ProjectNode>> getChildren,
      int recursionGuard)
    {
      int level;
      if(cache.TryGetValue(node.Id, out level))
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
      cache[node.Id] = level;
      return level;
    }

    private void RegisterDependence(ProjectNode depender, ProjectNode dependee)
    {
      depender.RegisterDependenceOn(dependee);
      dependee.RegisterDependentOf(depender);
    }
  }

  /// <summary>
  /// A node in the project dependency graph
  /// </summary>
  public class ProjectNode
  {
    private List<ProjectNode> _dependsOn;
    private List<ProjectNode> _dependentOf;

    internal ProjectNode(ProjectDependencyGraph owner, ProjectDetails project)
    {
      Owner = owner;
      Project = project;
      _dependsOn = new List<ProjectNode>();
      _dependentOf = new List<ProjectNode>();
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
    public IReadOnlyList<ProjectNode> DependsOn { get; }

    /// <summary>
    /// The list of nodes that depend on this project
    /// </summary>
    public IReadOnlyList<ProjectNode> DependentOf { get; }

    /// <summary>
    /// True if this is a leaf node, not depending on any other nodes
    /// </summary>
    public bool IsLeaf { get { return DependsOn.Count == 0; } }

    /// <summary>
    /// True if this is a root node, that no other nodes depend on
    /// </summary>
    public bool IsRoot { get { return DependentOf.Count == 0; } }

    /// <summary>
    /// The id of this node's project
    /// </summary>
    public Guid Id { get { return Project.Id; } }

    /// <summary>
    /// Get the project label
    /// </summary>
    public string Label { get { return Project.Label; } }

    /// <summary>
    /// Return the project ID in a forma that is safe to use as identifier
    /// </summary>
    public string IdString { get { return "X" + Project.Id.ToString("N"); } }

    /// <summary>
    /// True if this is a node of a stub project (such as a solution folder)
    /// that is not likely to be truly part of the dependency graph
    /// </summary>
    public bool IsStub { get { return Project.IsStub; } }

    /// <summary>
    /// Look up the value for this node in the specified map, returning the
    /// specified default value if not found
    /// </summary>
    public T Lookup<T>(Dictionary<Guid, T> map, T defaultValue)
    {
      T t;
      if (map.TryGetValue(Id, out t))
      {
        return t;
      }
      return defaultValue;
    }

    /// <summary>
    /// Look up the value for this node in the specified map, throwing 
    /// an exception if not found
    /// </summary>
    public T Lookup<T>(Dictionary<Guid, T> map)
    {
      return map[Id];
    }

    internal void RegisterDependenceOn(ProjectNode target)
    {
      _dependsOn.Add(target);
    }

    internal void RegisterDependentOf(ProjectNode source)
    {
      _dependentOf.Add(source);
    }

  }

}

