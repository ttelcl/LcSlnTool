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
/// A node in the solution tree, ready to serialize
/// </summary>
public class SolutionTreeNode
{
  private readonly List<SolutionTreeNode> _children;

  /// <summary>
  /// Create a new SolutionTreeNode
  /// </summary>
  public SolutionTreeNode(
    string name,
    IEnumerable<SolutionTreeNode> children,
    Guid id,
    Guid typeid,
    string type)
  {
    _children = [.. children];
    Children = _children.AsReadOnly();
    Name = name;
    Id = id;
    ProjectTypeId = typeid;
    ProjectType = type;
  }

  /// <summary>
  /// Recursively create a SolutionTreeNode from a SolutionProjectInfo
  /// instance.
  /// </summary>
  public static SolutionTreeNode FromSolutionProjectInfo(
    SolutionProjectInfo spi)
  {
    return FromSolutionProjectInfo(spi, 5);
  }

  private static SolutionTreeNode FromSolutionProjectInfo(
    SolutionProjectInfo spi,
    int recursionGuard)
  {
    if(recursionGuard <= 0)
    {
      throw new NotSupportedException(
        $"Unsupported deep nesting of Solution Folders. Aborting.");
    }
    var children =
      from childSpi in spi.ChildProjects
      orderby childSpi.Label
      select FromSolutionProjectInfo(childSpi, recursionGuard-1);
    return new SolutionTreeNode(
      spi.Label,
      children,
      spi.Id,
      spi.ProjectTypeId,
      spi.ProjectTypeName);
  }

  /// <summary>
  /// The project name
  /// </summary>
  [JsonProperty("name")]
  public string Name { get; }

  /// <summary>
  /// The project GUID (used to identify the project in the solution)
  /// </summary>
  [JsonProperty("id")]
  public Guid Id {  get; }

  /// <summary>
  /// The friendly name of the project type, if known. A name
  /// derived from the project type id otherwise
  /// </summary>
  [JsonProperty("type")]
  public string ProjectType { get; }

  /// <summary>
  /// The project type id
  /// </summary>
  [JsonProperty("typeid")]
  public Guid ProjectTypeId { get; }


  /// <summary>
  /// Child projects, if any. Only appears for Solution Folder 'projects'
  /// </summary>
  [JsonProperty("children")]
  public IReadOnlyList<SolutionTreeNode> Children { get; }

  /// <summary>
  /// Used by the serializer to determin if the 'Children' field
  /// should be serialized at all
  /// </summary>
  public bool ShouldSerializeChildren()
  {
    return Children.Count > 0;
  }

}
