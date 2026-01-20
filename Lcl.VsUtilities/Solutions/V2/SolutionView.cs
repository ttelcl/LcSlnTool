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
/// A solution-oriented view of a set of solution-project
/// relations
/// </summary>
public class SolutionView
{
  /// <summary>
  /// Create a new SolutionView (deserialization constructor)
  /// </summary>
  public SolutionView(
    IReadOnlyDictionary<string, IReadOnlyList<SolutionFile>> solutions)
  {
    SolutionMap = new Dictionary<string, IReadOnlyList<SolutionFile>>(
      solutions, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Create a new SolutionView from a set of
  /// <see cref="SolutionFile"/> objects; optionally load and link
  /// them.
  /// </summary>
  /// <param name="solutionFiles"></param>
  /// <param name="prepare">
  /// If true, <paramref name="solutionFiles"/> is assumed to contain
  /// nascent <see cref="SolutionFile"/> instances that still need
  /// Loading and Linking.
  /// </param>
  public static SolutionView FromSolutions(
    IEnumerable<SolutionFile> solutionFiles,
    bool prepare)
  {
    if(prepare)
    {
      foreach(var solutionFile in solutionFiles)
      {
        solutionFile.Load();
      }
      var supportedSolutions = solutionFiles.Where(sf => sf.HasSupportedProjects).ToList();
      var groupedByName =
        from solutionFile in supportedSolutions
        group solutionFile by solutionFile.SolutionName.ToLowerInvariant();
      foreach(var solutionGroup in groupedByName)
      {
        var solutions = solutionGroup.ToList();
        for(var i = 0; i <solutions.Count; i++)
        {
          var sf = solutions[i];
          if(solutions.Count > 1)
          {
            sf.Index = i+1; // side effect: change sf.Id to include index
          }
          else
          {
            sf.Index = 0; // side effect: sf.Id now == sf.SolutionName
          }
        }
      }
      // Only now the solution IDs are assigned and the project file references can be
      // converted into the V2 form
      foreach(var solutionFile in supportedSolutions)
      {
        solutionFile.LinkProjects();
      }
      solutionFiles = supportedSolutions;
    }
    var grouped =
      solutionFiles.GroupBy(x => x.SolutionName);
    var map =
      grouped.ToDictionary(
        g => g.Key, g => (IReadOnlyList<SolutionFile>)(g.ToList()),
        StringComparer.OrdinalIgnoreCase);
    return new SolutionView(map);
  }

  /// <summary>
  /// The collection of solutions, mapping solution names to one or more
  /// solution files. Normally each solution name is expected to be mapped
  /// to one solution file.
  /// </summary>
  [JsonProperty("solutions")]
  public IReadOnlyDictionary<string, IReadOnlyList<SolutionFile>> SolutionMap { get; }

  /// <summary>
  /// Enumerate all solutions nested in <see cref="SolutionMap"/>
  /// </summary>
  [JsonIgnore]
  public IEnumerable<SolutionFile> Solutions =>
    SolutionMap.Values.SelectMany(solutions => solutions);

  /// <summary>
  /// Enumerate and sort the projects nested in <see cref="SolutionMap"/>
  /// </summary>
  /// <returns></returns>
  public IEnumerable<ProjectFile2> EnumProjectsSorted()
  {
    var projects = Solutions.SelectMany(sf => sf.RecognizedProjects);
    var sortedProjects =
      from project in projects
      orderby project.Label.ToLowerInvariant(), project.SolutionId.ToLowerInvariant()
      select project;
    return sortedProjects;
  }
}
