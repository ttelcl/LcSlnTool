using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Lcl.VsUtilities.VirtualPaths;

/// <summary>
/// A virtual path instance, described as a pair of a virtual root alias and
/// a (virtual) path relative to the virtual root.
/// </summary>
public class VirtualPath
{
  /// <summary>
  /// Create a <see cref="VirtualPath"/>
  /// </summary>
  /// <param name="root"></param>
  /// <param name="path"></param>
  public VirtualPath(string root, string path)
  {
    RootKey = root;
    VPath = path;
  }

  /// <summary>
  /// The alias identifying the virtual root
  /// </summary>
  [JsonProperty("root")]
  public string RootKey { get; }

  /// <summary>
  /// The virtual path relative to the root referenced by <see cref="RootKey"/>
  /// </summary>
  [JsonProperty("path")]
  public string VPath { get; }

  /// <summary>
  /// Combines <see cref="RootKey"/> and <see cref="VPath"/> into a single string
  /// </summary>
  /// <returns></returns>
  public override string ToString()
  {
    return $"<{RootKey}>:{VPath}";
  }
}
