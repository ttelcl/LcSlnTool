using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xunit;
using Xunit.Abstractions;

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
    //_output.WriteLine($"Dir is {Environment.CurrentDirectory}");
    var slnPath = Path.GetFullPath(
      "..\\..\\..\\..\\LcSlnTool.sln");
    Assert.True(File.Exists(slnPath));
    var solutionInfo = SolutionInfo.FromFile(slnPath);
    Assert.NotNull(solutionInfo);
  }
}
