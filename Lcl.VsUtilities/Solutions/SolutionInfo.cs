/*
 * (c) 2017 ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Lcl.VsUtilities.Solutions
{
  /// <summary>
  /// Description of SolutionInfo
  /// </summary>
  public class SolutionInfo
  {
    private List<SolutionProjectInfo> _projects;

    /// <summary>
    /// Create a new SolutionInfo
    /// </summary>
    public SolutionInfo(string path, IEnumerable<SolutionProjectInfo> projects)
    {
      SolutionFile = Path.GetFullPath(path);
      SolutionFolder = Path.GetDirectoryName(SolutionFile);
      Name = Path.GetFileNameWithoutExtension(SolutionFile);
      _projects = projects==null ? new List<SolutionProjectInfo>() : new List<SolutionProjectInfo>(projects);
      Projects = _projects.AsReadOnly();
    }

    /// <summary>
    /// Loads a solution file, extracting information relevant to this library
    /// </summary>
    public static SolutionInfo FromFile(string solutionFileName)
    {
      if(String.IsNullOrEmpty(solutionFileName))
      {
        throw new ArgumentException("Expecting a non-empty file name", nameof(solutionFileName));
      }
      var hadHeader = false;
      var projects = new List<SolutionProjectInfo>();

      try
      {
        foreach (var line in File.ReadLines(solutionFileName))
        {
          var stripped = line.Trim();
          if (String.IsNullOrEmpty(stripped) || stripped.StartsWith("#"))
          {
            continue;
          }
          if (!hadHeader && line.StartsWith("Microsoft Visual Studio Solution File, Format Version"))
          {
            hadHeader = true;
            continue;
          }
          if (!hadHeader)
          {
            throw new InvalidOperationException("This file does not look like a VS solution file");
          }
          if (line.StartsWith("Project("))
          {
            var prj = SolutionProjectInfo.ParseHeader(line);
            if (prj != null)
            {
              projects.Add(prj);
            }
            else
            {
              throw new InvalidOperationException("Unable to parse line: " + line);
            }
          }
        }
        return new SolutionInfo(solutionFileName, projects);
      }
      catch (Exception ex)
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
    public string TryProjectFile(SolutionProjectInfo spi)
    {
      var fnm = Path.Combine(SolutionFolder, spi.Path);
      if(File.Exists(fnm))
      {
        return fnm;
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Try to load the project file for the specified project.
    /// If not available this returns null if "createStubs" is false, a stub
    /// item if it is true.
    /// </summary>
    public ProjectDetails LoadProject(SolutionProjectInfo spi, bool createStubs=false)
    {
      if(spi==null
        || spi.ProjectTypeId==ProjectTypes.SolutionFolder
        || spi.ProjectTypeId==ProjectTypes.SetupProject)
      {
        return createStubs ? new ProjectDetails(spi, null) : null;
      }
      var name = TryProjectFile(spi);
      if(String.IsNullOrEmpty(name))
      {
        return createStubs ? new ProjectDetails(spi, null) : null;
      }
      var prjf = ProjectFile.ParseFile(name);
      return new ProjectDetails(spi, prjf);
    }

  }
}

