/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.VsUtilities.Solutions;

/// <summary>
/// Utility to remove the current directory from paths that start with it
/// </summary>
public static class PathStripper
{
  /// <summary>
  /// If the path starts with the current directory, remove the current
  /// directory. Otherwise return the full path
  /// </summary>
  public static string RelativeToCurrentDirectory(string path)
  {
    var cd = Environment.CurrentDirectory;
    if(!cd.EndsWith(Path.DirectorySeparatorChar))
    {
      cd += Path.DirectorySeparatorChar;
    }
    path = Path.GetFullPath(path);
    if(path.StartsWith(cd, StringComparison.OrdinalIgnoreCase))
    {
      return path.Substring(cd.Length);
    }
    else
    {
      return path;
    }
  }

}
