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

namespace Lcl.VsUtilities.Solutions.V2;

/// <summary>
/// Locates files matching a pattern anywhere in a directory tree
/// </summary>
public static class FileFinder
{
  /// <summary>
  /// Recursively find files matching the pattern
  /// </summary>
  /// <param name="rootfolder"></param>
  /// <param name="pattern"></param>
  /// <returns></returns>
  public static IEnumerable<string> FindFilesRecursive(
    string rootfolder,
    string pattern)
  {
    return Directory.EnumerateFiles(rootfolder, pattern, SearchOption.AllDirectories);
  }

}
