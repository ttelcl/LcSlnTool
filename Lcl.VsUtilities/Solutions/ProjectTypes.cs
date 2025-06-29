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
  /// Utility for mapping project type GUIDs to names
  /// </summary>
  public static class ProjectTypes
  {
    /// <summary>
    /// The GUID of solution folders
    /// </summary>
    public static Guid SolutionFolder = Guid.Parse("2150e333-8fdc-42a3-9474-1a3956d46de8");

    /// <summary>
    /// The GUID of C# projects
    /// </summary>
    public static Guid CSharpProject = Guid.Parse("fae04ec0-301f-11d3-bf4b-00c04f79efbc");

    /// <summary>
    /// The GUID of F# projects
    /// </summary>
    public static Guid FSharpProject = Guid.Parse("f2a71f9b-5d33-465a-a702-920d77279786");
    /// <summary>
    /// The GUID of setup projects (old VS2010 era things, not XML)
    /// </summary>
    public static Guid SetupProject = Guid.Parse("54435603-DBB4-11D2-8724-00A0C9A8B90C");

    /// <summary>
    /// Get a name for a project type
    /// </summary>
    public static string ProjectTypeName(Guid projectType)
    {
      string name;
      if(__typeRegistry.TryGetValue(projectType, out name))
      {
        return name;
      }
      else
      {
        return $"ProjectType({projectType})";
      }
    }

    private static Dictionary<Guid, string> __typeRegistry = new Dictionary<Guid, string>();

    static ProjectTypes()
    {
      __typeRegistry[SolutionFolder] = "Solution Folder";
      __typeRegistry[CSharpProject] = "C# Project";
      __typeRegistry[FSharpProject] = "F# Project";
      __typeRegistry[SetupProject] = "Setup Project";
      __typeRegistry[Guid.Parse("8bc9ceb8-8b4a-11d0-8d11-00a0c91bc942")] = "C++ Project";
    }


  }
}

