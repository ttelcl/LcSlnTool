using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xunit;
using Xunit.Abstractions;

using Newtonsoft.Json;

using Lcl.VsUtilities.Solutions;

namespace UnitTests.VsUtilities;

public class SolutionTests
{
  private readonly ITestOutputHelper _output;

  public SolutionTests(ITestOutputHelper output)
  {
    _output = output;
  }

  [Fact]
  public void CanLoadSelfSolutionInfo()
  {
    var slnPath = Path.GetFullPath(
      "..\\..\\..\\..\\LcSlnTool.sln");
    Assert.True(File.Exists(slnPath));
    var solutionInfo = SolutionInfo.FromFile(slnPath);
    Assert.NotNull(solutionInfo);
    var tree = solutionInfo.BuildSolutionTree();
    var json = JsonConvert.SerializeObject(tree, Formatting.Indented);
    _output.WriteLine(json);
  }

  [Fact]
  public void CanloadSelfSolution()
  {
    var slnPath = Path.GetFullPath("..\\..\\..\\..\\LcSlnTool.sln");
    Assert.True(File.Exists(slnPath));

    var solution = new Solution(slnPath);
    Assert.NotNull(solution);
  }

  [Fact]
  public void CanDetermineDependencies()
  {
    var slnPath = Path.GetFullPath("..\\..\\..\\..\\LcSlnTool.sln");

    Assert.True(File.Exists(slnPath));
    DetermineDependencies(slnPath);
  }

  private void DetermineDependencies(string slnPath)
  {
    var solution = new Solution(slnPath);
    Assert.NotNull(solution);

    var graph = new ProjectDependencyGraph(solution);
    Assert.NotNull(graph);
    graph.StripSingletonStubs();

    var summaries = solution.BuildProjectSummaries(graph);
    Assert.NotNull(summaries);

    var json = JsonConvert.SerializeObject(summaries, Formatting.Indented);
    _output.WriteLine(json);
  }
}
