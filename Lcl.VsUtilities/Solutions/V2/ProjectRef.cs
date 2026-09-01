/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lcl.VsUtilities.Solutions.V2;

/// <summary>
/// A reference from one project to another
/// </summary>
public class ProjectRef
{
  /// <summary>
  /// Create a new ProjectRef
  /// </summary>
  public ProjectRef(
    string source,
    string target,
    [JsonProperty("type")] ReferenceType refType,
    [JsonProperty("intern")] bool isInternal = false,
    string version = "")
  {
    SourceProject = source;
    TargetProject = target;
    RefType = refType;
    IsInternal = isInternal;
    Version = version;
  }

  /// <summary>
  /// The name of the source project (that references <see cref="TargetProject"/>)
  /// </summary>
  [JsonProperty("source")]
  public string SourceProject { get; }

  /// <summary>
  /// The name of the target project (referenced by <see cref="SourceProject"/>).
  /// For now we ignore versions.
  /// </summary>
  [JsonProperty("target")]
  public string TargetProject { get; }

  /// <summary>
  /// The type of reference
  /// </summary>
  [JsonProperty("type")]
  [JsonConverter(typeof(StringEnumConverter))]
  public ReferenceType RefType { get; }

  /// <summary>
  /// Whether the target project is part of the same scan. Mutable.
  /// Initially false.
  /// </summary>
  [JsonProperty("intern")]
  public bool IsInternal { get; set; }

  /// <summary>
  /// The version of the target, if known, an empty string otherwise
  /// </summary>
  [JsonProperty("version", DefaultValueHandling = DefaultValueHandling.Ignore)]
  [DefaultValue("")]
  public string Version { get; }

  /// <summary>
  /// Find references in the given source project file
  /// </summary>
  /// <param name="sourceName"></param>
  /// <param name="sourceFile"></param>
  /// <param name="internalNames">
  /// The names of projects to be considered "internal"
  /// </param>
  /// <returns></returns>
  public static IEnumerable<ProjectRef> ReadReferences(
    string sourceName,
    string sourceFile,
    IReadOnlySet<string> internalNames)
  {
    var doc = new XPathDocument(sourceFile);
    var root = doc.CreateNavigator();
    var nsm = new XmlNamespaceManager(root.NameTable);
    nsm.AddNamespace("msb", MsbuildNamespace);
    var projectNodeLegacy = root.SelectSingleNode("/msb:Project", nsm);
    if(projectNodeLegacy != null)
    {
      return ReadReferencesLegacy(
        sourceName,
        nsm,
        root,
        internalNames);
    }
    else
    {
      return ReadReferencesSdkStyle(
        sourceName,
        nsm,
        root,
        internalNames);
    }
  }

  private static IEnumerable<ProjectRef> ReadReferencesLegacy(
    string sourceName,
    XmlNamespaceManager nsm,
    XPathNavigator root,
    IReadOnlySet<string> internalNames)
  {
    var projectReferences = root.Select("//msb:ProjectReference", nsm);
    foreach(XPathNavigator node in projectReferences)
    {
      var name = (string)node.Evaluate("string(msb:Name)", nsm);
      var an = new AssemblyName(name);
      name = an.Name ?? name;
      var version = an.Version?.ToString() ?? String.Empty; // usually Version==null, so result is String.Empty
      var pr = new ProjectRef(
        sourceName,
        name,
        ReferenceType.Project,
        internalNames.Contains(name),
        version);
      yield return pr;
    }
    var packageReferences = root.Select("//msb:PackageReference", nsm);
    foreach(XPathNavigator node in packageReferences)
    {
      var name = node.GetAttribute("Include", "");
      name = name.Split(',')[0]; // not expecting anything more. Expect a <Version> child element instead
      var version = (string)node.Evaluate("string(msb:Version)", nsm);
      var pr = new ProjectRef(
        sourceName,
        name,
        ReferenceType.Package,
        internalNames.Contains(name),
        version);
      yield return pr;
    }
    var plainReferences = root.Select("//msb:Reference", nsm);
    foreach(XPathNavigator node in plainReferences)
    {
      var name = node.GetAttribute("Include", "");
      var an = new AssemblyName(name);
      name = an.Name ?? name;
      var version = an.Version?.ToString() ?? string.Empty;
      var pr = new ProjectRef(
        sourceName,
        name,
        ReferenceType.Plain,
        internalNames.Contains(name),
        version);
      yield return pr;
    }
  }

  private static IEnumerable<ProjectRef> ReadReferencesSdkStyle(
    string sourceName,
    XmlNamespaceManager nsm,
    XPathNavigator root,
    IReadOnlySet<string> internalNames)
  {
    var projectNodeSdk = root.SelectSingleNode("/Project", nsm);
    if(projectNodeSdk == null)
    {
      throw new InvalidOperationException(
        $"Unrecognized project format in project '{sourceName}'");
    }
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
      name = name.Split(',')[0]; // just in case
      var version = (string?)node.Evaluate("string(Version)", nsm) ?? "";
      var pr = new ProjectRef(
        sourceName,
        name,
        ReferenceType.Project,
        internalNames.Contains(name),
        version);
      yield return pr;
    }
    var packageReferences = root.Select("//PackageReference");
    foreach(XPathNavigator node in packageReferences)
    {
      var name = node.GetAttribute("Include", "");
      var version = node.GetAttribute("Version", "");
      var pr = new ProjectRef(
        sourceName,
        name,
        ReferenceType.Package,
        internalNames.Contains(name),
        version);
      yield return pr;
    }
    // there are no plain references, isn't it?
  }

  private const string MsbuildNamespace =
    "http://schemas.microsoft.com/developer/msbuild/2003";

}
