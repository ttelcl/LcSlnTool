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
/// Create forward or reverse dependency "trees"
/// </summary>
public static class DependencyReport
{
  /// <summary>
  /// Create forward dependency trees for all projects in the graph
  /// </summary>
  public static Dictionary<string, DependsOnNode> MakeDependsOnReport(ProjectDependencyGraph pdg)
  {
    var report = new Dictionary<string, DependsOnNode>(StringComparer.OrdinalIgnoreCase);
    foreach(var pn in pdg.Nodes)
    {
      GetDependsOnNode(report, pn, 32);
    }
    return report;
  }

  /// <summary>
  /// Create reverse dependency trees for all projects in the graph
  /// </summary>
  public static Dictionary<string, DependentOfNode> MakeDependentOfReport(ProjectDependencyGraph pdg)
  {
    var report = new Dictionary<string, DependentOfNode>(StringComparer.OrdinalIgnoreCase);
    foreach(var pn in pdg.Nodes)
    {
      GetDependentOfNode(report, pn, 32);
    }
    return report;
  }

  private static DependsOnNode GetDependsOnNode(
    Dictionary<string, DependsOnNode> cache, GraphProjectNode source, int maxRecurse)
  {
    if(cache.TryGetValue(source.Label, out var dn))
    {
      return dn;
    }
    if(maxRecurse <= 0)
    {
      throw new InvalidOperationException("Recursion limit exceeded");
    }
    var list = new List<DependsOnNode>();
    foreach(var child in source.DependsOn)
    {
      list.Add(GetDependsOnNode(cache, child, maxRecurse - 1));
    }
    dn = new DependsOnNode(source.Project.Label, list);
    cache[source.Label] = dn;
    return dn;
  }

  private static DependentOfNode GetDependentOfNode(
    Dictionary<string, DependentOfNode> cache, GraphProjectNode source, int maxRecurse)
  {
    if(cache.TryGetValue(source.Label, out var dn))
    {
      return dn;
    }
    if(maxRecurse <= 0)
    {
      throw new InvalidOperationException("Recursion limit exceeded");
    }
    var list = new List<DependentOfNode>();
    foreach(var child in source.DependentOf)
    {
      list.Add(GetDependentOfNode(cache, child, maxRecurse - 1));
    }
    dn = new DependentOfNode(source.Project.Label, list);
    cache[source.Label] = dn;
    return dn;
  }
}

/// <summary>
/// Minimalistic node in dependency hierarchy for JSON serialization
/// </summary>
public class DependsOnNode
{
  /// <summary>
  /// Create a DependsOnNode
  /// </summary>
  public DependsOnNode(
    string name,
    IEnumerable<DependsOnNode> dependsOn)
  {
    Name = name;
    DependsOn = new List<DependsOnNode>(dependsOn).AsReadOnly();
  }

  /// <summary>
  /// The project name
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// The dependencies, possibly null
  /// </summary>
  public IReadOnlyList<DependsOnNode> DependsOn { get; }

  /// <summary>
  /// Create a dense representation of this node. Note that the return type is actually
  /// the the infinitely recursive type Dictionary{string,Dictionary{string,Dictionary{string,...}}}
  /// </summary>
  public Dictionary<string, object> DenseRepresentation()
  {
    var ret = new Dictionary<string, object>();
    ret[Name] = DenseContentRepresentation();
    return ret;
  }

  private Dictionary<string, object> DenseContentRepresentation()
  {
    var ret = new Dictionary<string, object>();
    foreach(var n in DependsOn)
    {
      ret[n.Name] = n.DenseContentRepresentation();
    }
    return ret;
  }
}

/// <summary>
/// Minimalistic node in reverse dependency hierarchy for JSON serialization
/// </summary>
public class DependentOfNode
{
  /// <summary>
  /// Create a DependsOnNode
  /// </summary>
  public DependentOfNode(
    string name,
    IEnumerable<DependentOfNode> dependsOn)
  {
    Name = name;
    DependentOf = new List<DependentOfNode>(dependsOn).AsReadOnly();
  }

  /// <summary>
  /// The project name
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// The dependencies, possibly null
  /// </summary>
  public IReadOnlyList<DependentOfNode> DependentOf { get; }
}

