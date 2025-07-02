/*
 * (c) 2017 ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using System.Xml;
using System.IO;

namespace Lcl.VsUtilities.Solutions;

/// <summary>
/// Stores a subset of the information in an MSBUILD-style project file
/// (*.csproj, *.fsproj)
/// </summary>
public class ProjectFile
{
  /// <summary>
  /// Create a new ProjectFile
  /// </summary>
  public ProjectFile(
    IEnumerable<ProjectReference> prjrefs,
    string? sdk,
    IEnumerable<string> frameworks)
  {
    Sdk = sdk;
    ProjectReferences = prjrefs.ToList().AsReadOnly();
    Frameworks = frameworks.ToList().AsReadOnly();
  }

  /// <summary>
  /// The project references
  /// </summary>
  public IReadOnlyList<ProjectReference> ProjectReferences { get; }

  /// <summary>
  /// The SDK name for SDK style projects, or null for legacy and dummy projects.
  /// </summary>
  public string? Sdk { get; }

  /// <summary>
  /// Target framework(s)
  /// </summary>
  public IReadOnlyList<string> Frameworks { get; }

  const string MsbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

  /// <summary>
  /// Load a project file, if it exists as a file
  /// </summary>
  public static ProjectFile ParseFile(string filename, SolutionInfo si)
  {
    if(String.IsNullOrEmpty(filename))
    {
      throw new ArgumentException("Expecting a non-empty file name", nameof(filename));
    }

    try
    {
      var doc = new XPathDocument(filename);
      var root = doc.CreateNavigator();
      var nsm = new XmlNamespaceManager(root.NameTable);
      nsm.AddNamespace("msb", MsbuildNamespace);
      var projectNodeLegacy = root.SelectSingleNode("/msb:Project", nsm);
      if(projectNodeLegacy != null)
      {
        // Legacy (non-'SDK') project file. 
        var projectReferences = root.Select("//msb:ProjectReference", nsm);
        var prjrefs = new List<ProjectReference>();
        foreach(XPathNavigator node in projectReferences)
        {
          var include = node.GetAttribute("Include", "");
          var projectText = (string)node.Evaluate("string(msb:Project)", nsm);
          var name = (string)node.Evaluate("string(msb:Name)", nsm);
          var project = Guid.Parse(projectText);
          prjrefs.Add(new ProjectReference(name, project, include));
        }
        var targetNodes = root.Select("//msb:TargetFrameworkVersion", nsm);
        var targets = new HashSet<string>();
        foreach(XPathNavigator targetNode in targetNodes)
        {
          var txt = (string)targetNode.Evaluate("string(.)", nsm);
          targets.Add(txt);
        }
        return new ProjectFile(prjrefs, null, targets);
      }
      else
      {
        // SDK style project. Note that this has no concept of 'project GUID'
        // directly, nor in project references. For uniqueness we need those
        // though, so we need to get that information from the solution info object.
        var projectNodeSdk = root.SelectSingleNode("/Project", nsm);
        if(projectNodeSdk == null)
        {
          throw new InvalidOperationException(
            $"Unrecognized project format in project file {filename}");
        }
        root = doc.CreateNavigator();
        var sdk = projectNodeSdk.GetAttribute("Sdk", String.Empty);
        var prjrefs = new List<ProjectReference>();
        var projectReferences = root.Select("//ProjectReference");
        foreach(XPathNavigator node in projectReferences)
        {
          var include = node.GetAttribute("Include", "");
          var name = (string?)node.Evaluate("string(Name)", nsm);
          if(String.IsNullOrEmpty(name))
          {
            // This is the expected code path.
            name = Path.GetFileNameWithoutExtension(include);
          }
          var refSpi = si.FindProjectInfoForProjectFile(include);
          if(refSpi == null)
          {
            throw new InvalidOperationException(
              $"Internal error: Project file not found in solution file: '{include}'");
          }
          prjrefs.Add(new ProjectReference(name, refSpi.Id, include));
        }
        var targetNodes = projectNodeSdk.Select("PropertyGroup/TargetFrameworks");
        var targets = new HashSet<string>();
        foreach(XPathNavigator targetNode in targetNodes)
        {
          var txt = (string)targetNode.Evaluate("string(.)", nsm);
          if(!String.IsNullOrEmpty(txt))
          {
            foreach(var target in txt.Split(';'))
            {
              targets.Add(target.Trim());
            }
          }
        }
        targetNodes = projectNodeSdk.Select("PropertyGroup/TargetFramework");
        foreach(XPathNavigator targetNode in targetNodes)
        {
          var txt = (string)targetNode.Evaluate("string(.)", nsm);
          if(!String.IsNullOrEmpty(txt))
          {
            targets.Add(txt);
          }
        }
        return new ProjectFile(prjrefs, sdk, targets);
      }
    }
    catch(Exception ex)
    {
      throw new InvalidOperationException(
        $"Error while loading project file '{filename}'",
        ex);
    }
  }

}

