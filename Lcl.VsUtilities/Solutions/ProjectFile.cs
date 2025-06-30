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
    string? sdk)
  {
    Sdk = sdk;
    var pr = new List<ProjectReference>(prjrefs);
    ProjectReferences = pr.AsReadOnly();
  }

  /// <summary>
  /// The project references
  /// </summary>
  public IReadOnlyList<ProjectReference> ProjectReferences { get; }

  /// <summary>
  /// The SDK name for SDK style projects, or null for legacy and dummy projects.
  /// </summary>
  public string? Sdk { get; }

  const string MsbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

  /// <summary>
  /// Load a project file
  /// </summary>
  public static ProjectFile ParseFile(string filename)
  {
    if (String.IsNullOrEmpty(filename))
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
        var projectReferences = root.Select("//msb:ProjectReference", nsm);
        var prjrefs = new List<ProjectReference>();
        foreach(XPathNavigator node in projectReferences)
        {
          var include = node.GetAttribute("Include", "");
          var projectText = (string)node.Evaluate("string(msb:Project)", nsm);
          var name = (string)node.Evaluate("string(msb:Name)", nsm);
          var project = Guid.Parse(projectText);
          prjrefs.Add(new ProjectReference(name, /*project,*/ include));
        }
        return new ProjectFile(prjrefs, null);
      }
      else
      {
        var projectNodeSdk = root.SelectSingleNode("/Project", nsm);
        if(projectNodeSdk == null)
        {
          throw new InvalidOperationException(
            $"Unrecognized project format in project file {filename}");
        }
        var sdk = projectNodeSdk.GetAttribute("Sdk", String.Empty);
        var prjrefs = new List<ProjectReference>();
        var projectReferences = root.Select("//ProjectReference", nsm);
        foreach(XPathNavigator node in projectReferences)
        {
          var include = node.GetAttribute("Include", "");
          //var projectText = (string)node.Evaluate("string(msb:Project)", nsm);
          var name = (string?)node.Evaluate("string(Name)", nsm);
          if(String.IsNullOrEmpty(name))
          {
            // This is the expected code path.
            name = Path.GetFileNameWithoutExtension(include);
          }
          //var project = Guid.Parse(projectText);
          prjrefs.Add(new ProjectReference(name, /*project,*/ include));
        }
        // TODO: fill prjrefs
        return new ProjectFile(prjrefs, sdk);
      }
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException(
        $"Error while loading project file '{filename}'",
        ex);
    }
  }

}

