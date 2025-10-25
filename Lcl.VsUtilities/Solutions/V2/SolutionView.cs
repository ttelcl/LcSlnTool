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
  /// Create a new SolutionView from a prepared set of
  /// <see cref="SolutionFile"/> objects (loaded and linked)
  /// </summary>
  public SolutionView(
    IEnumerable<SolutionFile> solutions)
  {
    var grouped =
      solutions.GroupBy(x => x.SolutionName);
    var map =
      grouped.ToDictionary(
        g => g.Key, g => (IReadOnlyList<SolutionFile>)(g.ToList()));
    Solutions = map;
  }

  /// <summary>
  /// The collection of solutions, mapping solution names to one or more
  /// solution files. Normally each solution name is expected to be mapped
  /// to one solution file.
  /// </summary>
  [JsonProperty("solutions")]
  public IReadOnlyDictionary<string, IReadOnlyList<SolutionFile>> Solutions { get; }
}
