/*
 * (c) 2017 ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lcl.VsUtilities.Solutions;

/// <summary>
/// Partial description of a Project in a VS solution
/// </summary>
public class SolutionProjectInfo
{
  private static Regex __projectHeaderRegex =
    new Regex(@"^Project\(([^)]+)\)\s+=\s+([^,]+),\s+([^,]+),\s+(.+)\s*$");

  /// <summary>
  /// Create a new ProjectInfo
  /// </summary>
  public SolutionProjectInfo(
    Guid projectTypeId,
    string label,
    string path,
    Guid projectId)
  {
    ChildProjects = [];
    ProjectTypeId = projectTypeId;
    Label = label;
    Path = path;
    Id = projectId;
  }

  /// <summary>
  /// Creates a ProjectInfo from its header line text in the solution file
  /// </summary>
  public static SolutionProjectInfo? ParseHeader(string headerLine)
  {
    var m = __projectHeaderRegex.Match(headerLine);
    if(m.Success)
    {
      var typeGuidText = Unquote(m.Groups[1].Value)!;
      var label = Unquote(m.Groups[2].Value);
      var path = Unquote(m.Groups[3].Value);
      var guidText = Unquote(m.Groups[4].Value);
      var typeGuid = Guid.Parse(typeGuidText);
      var guid = Guid.Parse(guidText);
      return new SolutionProjectInfo(typeGuid, label, path, guid);
    }
    else
    {
      return null;
    }
  }

  private static string Unquote(string s)
  {
    if(/*s!=null &&*/ s.Length>=2 && s[0]=='"' && s[s.Length-1]=='"')
    {
      return s.Substring(1, s.Length - 2);
    }
    else
    {
      return s;
    }
  }

  /// <summary>
  /// Identifies the type of project
  /// </summary>
  public Guid ProjectTypeId { get; }

  /// <summary>
  /// Identifies the project
  /// </summary>
  public Guid Id { get; }

  /// <summary>
  /// Name for the project
  /// </summary>
  public string Label { get; }

  /// <summary>
  /// For most project types this is the relative path to the project file.
  /// May be something else for projects that don't have a project file
  /// (such as solution folders)
  /// </summary>
  public string Path { get; }

  /// <summary>
  /// Get a name for the project type
  /// </summary>
  public string ProjectTypeName => ProjectTypes.ProjectTypeName(ProjectTypeId);

  /// <summary>
  /// The solution folder 'project' this project is in, or null
  /// if it is not in a Solution Folder.
  /// </summary>
  public SolutionProjectInfo? SolutionFolderProject { get; set; }

  /// <summary>
  /// The 'path' in the solution tree of this project node
  /// </summary>
  public string SolutionTreePath()
  {
    return
      SolutionFolderProject == null
      ? "/" + Label
      : SolutionFolderProject.SolutionTreePath() + "/" + Label;
  }

  /// <summary>
  /// If this is a Solution Folder project, this list contains the projects
  /// in that solution folder
  /// </summary>
  public List<SolutionProjectInfo> ChildProjects { get; }
}

