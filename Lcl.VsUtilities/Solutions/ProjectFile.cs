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
  public ProjectFile(IEnumerable<ProjectReference>? prjrefs)
  {
    var pr = prjrefs==null
      ? new List<ProjectReference>()
      : new List<ProjectReference>(prjrefs);
    ProjectReferences = pr.AsReadOnly();
  }

  /// <summary>
  /// The project references
  /// </summary>
  public IReadOnlyList<ProjectReference> ProjectReferences { get; }

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
      nsm.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");
      var projectReferences = root.Select("//msb:ProjectReference", nsm);
      var prjrefs = new List<ProjectReference>();
      foreach (XPathNavigator node in projectReferences)
      {
        var include = node.GetAttribute("Include", "");
        var projectText = (string)node.Evaluate("string(msb:Project)", nsm);
        var name = (string)node.Evaluate("string(msb:Name)", nsm);
        var project = Guid.Parse(projectText);
        prjrefs.Add(new ProjectReference(name, project, include));
      }
      return new ProjectFile(prjrefs);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException(
        $"Error while loading project file '{filename}'",
        ex);
    }
  }

}

