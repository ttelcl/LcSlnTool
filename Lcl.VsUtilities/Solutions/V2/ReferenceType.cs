/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.VsUtilities.Solutions.V2;

/// <summary>
/// Different reference types
/// </summary>
public enum ReferenceType
{
  /// <summary>
  /// A plain reference (&lt;Reference&gt; element)
  /// </summary>
  Plain = 0,

  /// <summary>
  /// A project reference
  /// </summary>
  Project = 1,

  /// <summary>
  /// A package reference
  /// </summary>
  Package = 2,
}
