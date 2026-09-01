using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Lcl.VsUtilities.VirtualPaths;

/// <summary>
/// Tracks virtual path mappings and helps converting between virtual paths
/// and local full paths
/// </summary>
public class VirtualPathDb
{
  private readonly Dictionary<string, VirtualPathDefinition> _virtualpaths;
  private readonly Dictionary<string, string> _paths;
  private readonly List<VirtualPathDefinition> _orderedDefinitions;

  /// <summary>
  /// Create a new <see cref="VirtualPathDb"/> instance and register a virtual path
  /// definition for the root itself.
  /// </summary>
  /// <param name="defaultAlias"></param>
  /// <param name="rootPath"></param>
  public VirtualPathDb(
    string defaultAlias,
    string? rootPath = null)
  {
    _orderedDefinitions = new List<VirtualPathDefinition>();
    _virtualpaths = new Dictionary<string, VirtualPathDefinition>(StringComparer.OrdinalIgnoreCase);
    _paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    RootPath = String.IsNullOrEmpty(rootPath) ? Environment.CurrentDirectory : Path.GetFullPath(rootPath);
    HideRootPath = String.IsNullOrEmpty(rootPath);
    RootPrefix =
      RootPath.EndsWith(Path.DirectorySeparatorChar)
      ? RootPath
      : RootPath + Path.DirectorySeparatorChar;
    RegisterDefinition(defaultAlias, RootPrefix);
  }

  /// <summary>
  /// The local root path, used as a common prefix for other local paths.
  /// By default this is the current directory at construction time.
  /// </summary>
  [JsonProperty("rootpath")]
  public string RootPath { get; }

  /// <summary>
  /// <see cref="RootPath"/> with a <see cref="Path.DirectorySeparatorChar"/> appended if it didn't have one.
  /// </summary>
  [JsonIgnore]
  public string RootPrefix { get; }

  /// <summary>
  /// The mapping from aliases to relative paths
  /// </summary>
  [JsonProperty("virtualroots")]
  public IReadOnlyDictionary<string, string> Paths => _paths;

  /// <summary>
  /// Try to match the given path to the defined virtual path definitions,
  /// returning the longest match, or null if there is no match.
  /// </summary>
  /// <param name="path"></param>
  /// <returns></returns>
  public VirtualPath? MatchPath(string path)
  {
    path = Expand(path);
    return
      _orderedDefinitions
      .Select(vpd => vpd.TryMatch(path))
      .FirstOrDefault();
  }

  /// <summary>
  /// Return the definition for the given alias (throwing an exception if not found)
  /// </summary>
  public VirtualPathDefinition GetDefinition(string alias)
  {
    return _virtualpaths[alias];
  }

  /// <summary>
  /// Return the definition for the alias of the given virtual path (throwing an exception if not found)
  /// </summary>
  public VirtualPathDefinition GetDefinition(VirtualPath vp)
  {
    return GetDefinition(vp.RootKey);
  }

  /// <summary>
  /// Reflectively used by Newtonsoft.Json to decide whether or not to serialize
  /// <see cref="RootPath"/>. This just exposes <see cref="HideRootPath"/> (inverted)
  /// </summary>
  /// <returns></returns>
  public bool ShouldSerializeRootPath() => !HideRootPath;

  /// <summary>
  /// Expand the given path relative to <see cref="RootPath"/>
  /// </summary>
  /// <param name="path"></param>
  /// <returns></returns>
  public string Expand(string path)
  {
    return Path.Combine(RootPath, path);
  }

  /// <summary>
  /// Whether or not to serialize <see cref="RootPath"/>. Initially true if an explicit root path
  /// was set in the constructor, false otherwise
  /// </summary>
  [JsonIgnore]
  public bool HideRootPath { get; set; }

  /// <summary>
  /// Register (or replace) a virtual path definition
  /// </summary>
  /// <param name="alias"></param>
  /// <param name="path"></param>
  /// <returns></returns>
  public VirtualPathDefinition RegisterDefinition(string alias, string path)
  {
    path = Path.Combine(RootPath, path);
    TryStripPath(path, out var strippedPath); // don't care about the return value
    return SetVirtualPath(alias, strippedPath);
  }
  
  /// <summary>
  /// Add or replace a virtual path record
  /// </summary>
  /// <param name="alias"></param>
  /// <param name="path"></param>
  private VirtualPathDefinition SetVirtualPath(string alias, string path)
  {
    var vpath = new VirtualPathDefinition(this, alias, path);
    if(_virtualpaths.TryGetValue(alias, out var oldDefinition))
    {
      _virtualpaths.Remove(alias);
      _paths.Remove(alias);
      // _orderedDefinitions.Remove(oldDefinition); // no need; will be removed anyway in .Clear()
    }
    _virtualpaths[alias] = vpath;
    _paths[alias] = vpath.Path;
    _orderedDefinitions.Clear();
    // Make sure that the longest paths are first in the list of virtual path definitions
    _orderedDefinitions.AddRange(
      from def in _virtualpaths.Values
      orderby def.Prefix.Length descending
      select def);
    return vpath;
  }

  /// <summary>
  /// Try to strip <see cref="RootPrefix"/> from the given path.
  /// </summary>
  /// <param name="path"></param>
  /// <param name="strippedPath"></param>
  /// <returns></returns>
  public bool TryStripPath(string path, out string strippedPath)
  {
    return TryStripPath(RootPrefix, path, out strippedPath);
  }

  /// <summary>
  /// Try to strip the <paramref name="prefix"/> from the <paramref name="path"/>.
  /// Both prefix and path are expected to be fully qualified paths, and prefix is
  /// expected to end with <see cref="Path.DirectorySeparatorChar"/>.
  /// </summary>
  /// <param name="prefix">
  /// The prefix to strip away
  /// </param>
  /// <param name="path">
  /// The path to strip the prefix away from
  /// </param>
  /// <param name="strippedPath">
  /// On success: the <paramref name="path"/> with the <paramref name="prefix"/> removed.
  /// In this case this may be an empty string.
  /// On failure: the <paramref name="path"/>, unchanged.
  /// </param>
  /// <returns>
  /// True on success, false on failure.
  /// </returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static bool TryStripPath(
    string prefix,
    string path,
    out string strippedPath)
  {
    if(!prefix.EndsWith(Path.DirectorySeparatorChar))
    {
      throw new InvalidOperationException(
        "Expecting prefix to end with a path separator");
    }
    if(!Path.IsPathFullyQualified(prefix) || !Path.IsPathRooted(prefix))
    {
      throw new InvalidOperationException(
        "Expecting 'prefix' to be fully qualified");
    }
    if(!Path.IsPathFullyQualified(path) || !Path.IsPathRooted(path))
    {
      throw new InvalidOperationException(
        "Expecting 'path' to be fully qualified");
    }
    if(!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
      strippedPath = path;
      return false;
    }
    strippedPath = path.Substring(prefix.Length);
    return true;
  }
}
