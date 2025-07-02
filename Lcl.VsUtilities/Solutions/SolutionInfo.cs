/*
 * (c) 2017 ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Lcl.VsUtilities.Solutions;

/// <summary>
/// Information found in a solution file
/// </summary>
public class SolutionInfo
{
  private List<SolutionProjectInfo> _projects;
  private Dictionary<string, SolutionProjectInfo> _projectByFileMap;

  /// <summary>
  /// Create a new SolutionInfo
  /// </summary>
  private SolutionInfo(
    string path,
    IEnumerable<SolutionProjectInfo> projects,
    string? vsVersion)
  {

    SolutionFile = Path.GetFullPath(path);
    SolutionFolder = Path.GetDirectoryName(SolutionFile)!;
    Name = Path.GetFileNameWithoutExtension(SolutionFile);
    _projects = [.. projects];
    _projectByFileMap = new Dictionary<string, SolutionProjectInfo>(
      StringComparer.OrdinalIgnoreCase);
    Projects = _projects.AsReadOnly();
    VisualStudioVersion = vsVersion ?? "0.0.0.0";
    foreach(var project in projects)
    {
      var projectFile = TryProjectFile(project);
      if(projectFile != null) 
      {
        // Only map true projects, not solution folders
        // This should remove the most common cause for name conflicts
        _projectByFileMap.Add(project.Key, project);
      }
    }
  }

  /// <summary>
  /// Loads a solution file, extracting information relevant to this library
  /// </summary>
  public static SolutionInfo FromFile(string solutionFileName)
  {
    if(String.IsNullOrEmpty(solutionFileName))
    {
      throw new ArgumentException(
        "Expecting a non-empty file name", nameof(solutionFileName));
    }
    var hadHeader = false;
    var projects = new List<SolutionProjectInfo>();
    string? vsVersion = null;
    var parsingNested = false;
    try
    {
      foreach(var line in File.ReadLines(solutionFileName))
      {
        var stripped = line.Trim();
        if(String.IsNullOrEmpty(stripped))
        {
          continue;
        }
        if(stripped.StartsWith('#'))
        {
          continue;
        }
        if(!hadHeader && stripped.StartsWith(
          "Microsoft Visual Studio Solution File, Format Version"))
        {
          hadHeader = true;
          continue;
        }
        if(!hadHeader)
        {
          throw new InvalidOperationException(
            "This file does not look like a VS solution file");
        }
        if(parsingNested)
        {
          if(stripped.StartsWith("EndGlobalSection"))
          {
            parsingNested = false;
            continue;
          }
          var parts =
            stripped.Split(
              '=',
              StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
          if(parts.Length != 2)
          {
            throw new InvalidOperationException(
              "Unexpected content in 'nested projects' section: expecting 1 '=' character");
          }
          if(!Guid.TryParse(parts[0], out var childId))
          {
            throw new InvalidOperationException(
              "Unexpected content in 'nested projects' section: first GUID is not valid");
          }
          var childProject =
            projects.FirstOrDefault(p => p.Id == childId)
            ?? throw new InvalidOperationException(
              $"Project nesting error: unknown child project {parts[0]}");
          if(!Guid.TryParse(parts[1], out var parentId))
          {
            throw new InvalidOperationException(
              "Unexpected content in 'nested projects' section: second GUID is not valid");
          }
          var parentProject =
            projects.FirstOrDefault(p => p.Id == parentId)
            ?? throw new InvalidOperationException(
              $"Project nesting error: unknown parent project (Solution Folder) {parts[1]}");
          childProject.SolutionFolderProject = parentProject;
          parentProject.ChildProjects.Add(childProject);
          continue;
        }
        if(stripped.StartsWith("GlobalSection(NestedProjects)"))
        {
          parsingNested = true;
          continue;
        }
        if(stripped.StartsWith("VisualStudioVersion"))
        {
          var parts =
            stripped.Split("=", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
          if(parts.Length != 2)
          {
            throw new InvalidOperationException(
              "Unexpected sytax for 'VisualStudioVersion' line");
          }
          vsVersion = parts[1];
          continue;
        }
        if(stripped.StartsWith("Project("))
        {
          var prj = SolutionProjectInfo.ParseHeader(line);
          if(prj != null)
          {
            projects.Add(prj);
          }
          else
          {
            throw new InvalidOperationException(
              "Unable to parse line: " + line);
          }
        }
      }
      var si = new SolutionInfo(
        solutionFileName,
        projects,
        vsVersion);
      return si;
    }
    catch(Exception ex)
    {
      throw new InvalidOperationException(
        $"Error while loading '{solutionFileName}'",
        ex);
    }
  }

  /// <summary>
  /// The solution name (derived from the file name)
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// The Visual Studio Version that wrote this solution file,
  /// or "0.0.0.0" for very old VS versions.
  /// </summary>
  public string VisualStudioVersion { get; }

  /// <summary>
  /// The full path to the solution file
  /// </summary>
  public string SolutionFile { get; }

  /// <summary>
  /// The full path to the solution folder
  /// </summary>
  public string SolutionFolder { get; }

  /// <summary>
  /// The collection of project infos
  /// </summary>
  public IReadOnlyList<SolutionProjectInfo> Projects { get; }

  /// <summary>
  /// Returns the full path to the project file if it exists, or null if it doesn't
  /// </summary>
  public string? TryProjectFile(SolutionProjectInfo spi)
  {
    var fnm = Path.Combine(SolutionFolder, spi.Path);
    if(File.Exists(fnm))
    {
      return fnm;
    }
    else // includes the case where spi.Path is a directory or solution folder
    {
      return null;
    }
  }

  /// <summary>
  /// Try to load the project file for the specified project.
  /// If not available this returns null if "createStubs" is false, a stub
  /// item if it is true.
  /// </summary>
  public ProjectDetails? LoadProject(SolutionProjectInfo spi, bool createStubs = false)
  {
    if(spi.ProjectTypeId==ProjectTypes.SolutionFolder
      || spi.ProjectTypeId==ProjectTypes.SetupProject)
    {
      return createStubs ? new ProjectDetails(spi, null) : null;
    }
    var name = TryProjectFile(spi);
    if(String.IsNullOrEmpty(name))
    {
      return createStubs ? new ProjectDetails(spi, null) : null;
    }
    var prjf = ProjectFile.ParseFile(name, this);
    return new ProjectDetails(spi, prjf);
  }

  /// <summary>
  /// Given a full or short project file name, find the matching
  /// <see cref="SolutionProjectInfo"/> (returning null if missing)
  /// </summary>
  public SolutionProjectInfo? FindProjectInfoForProjectFile(string projectFile)
  {
    var shortName = Path.GetFileName(projectFile);
    return
      _projectByFileMap.TryGetValue(shortName, out var spi) ? spi : null;
  }

  /// <summary>
  /// Build a JSON serializable model of the solution tree (a forest really,
  /// the root node is not included)
  /// </summary>
  /// <returns></returns>
  public IReadOnlyList<SolutionTreeNode> BuildSolutionTree()
  {
    var nodes =
      from project in Projects
      where project.SolutionFolderProject == null
      orderby project.Label
      select SolutionTreeNode.FromSolutionProjectInfo(project);
    return nodes.ToList().AsReadOnly();
  }
}

