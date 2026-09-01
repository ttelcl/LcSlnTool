using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.VsUtilities.VirtualPaths;

/// <summary>
/// A virtual path record
/// </summary>
public class VirtualPathDefinition
{
  
  internal VirtualPathDefinition(VirtualPathDb owner, string alias, string path)
  {
    Owner = owner;
    Alias = alias;
    Path = path;
    var prefix = Owner.Expand(path);
    Prefix =
      prefix.EndsWith(System.IO.Path.DirectorySeparatorChar)
      ? prefix
      : prefix + System.IO.Path.DirectorySeparatorChar;
  }

  /// <summary>
  /// The <see cref="VirtualPathDb"/> that owns this record
  /// </summary>
  public VirtualPathDb Owner { get; }

  /// <summary>
  /// The alias for this virtual path
  /// </summary>
  public string Alias { get; }

  /// <summary>
  /// The target path, relative to the owning <see cref="VirtualPathDb"/>'s
  /// <see cref="VirtualPathDb.RootPath"/>.
  /// </summary>
  public string Path { get; }

  /// <summary>
  /// The full prefix, including the root prefix, <see cref="Path"/>, and if missing,
  /// a final path separator
  /// </summary>
  public string Prefix { get; }

  /// <summary>
  /// Try to match this virtual path definition and return the resulting
  /// <see cref="VirtualPath"/> instance on success, null otherwise.
  /// </summary>
  /// <param name="path">
  /// The path to match. If relative, it is interpreted relative to the <see cref="Owner"/>'s
  /// root path (not relative to this definition's path).
  /// </param>
  /// <returns></returns>
  public VirtualPath? TryMatch(string path)
  {
    path = Owner.Expand(path);
    return
      VirtualPathDb.TryStripPath(Prefix, path, out var strippedPath)
      ? new VirtualPath(Alias, strippedPath)
      : null;
  }
}
