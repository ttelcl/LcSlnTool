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
/// Utility to help write a GraphViz *.dot file.
/// This does not support everything, just what is needed in the scope
/// of this library.
/// </summary>
public class DotFileWriter: IDisposable
{
  private TextWriter? _writer;
  private HashSet<string> _knownNodes;

  /// <summary>
  /// Create a new DotFileWriter
  /// </summary>
  public DotFileWriter(
    string fileName,
    bool directed,
    bool horizontal = false,
    string? graphId = null)
  {
    _knownNodes = new HashSet<string>(StringComparer.Ordinal);
    _writer = File.CreateText(fileName);
    Directed = directed;
    Writer.Write(Directed ? "digraph" : "graph");
    if(graphId != null)
    {
      Writer.Write($" \"{graphId}\"");
    }
    Writer.WriteLine(" {");
    Writer.WriteLine(horizontal ? "  rankdir=LR;" : "  rankdir=TB;");
  }

  /// <summary>
  /// Whether this is a directed or undirected graph
  /// </summary>
  public bool Directed { get; }

  /// <summary>
  /// Add a node
  /// </summary>
  public void AddNode(
    string label, // Must not include quotes.
    IEnumerable<string> sublabels, // written as italic lines below main label
    string? shape = "box",
    string? style = "filled",
    string? color = null, // background color, omitted if null
    string? id = null) // defaults to label
  {
    if(String.IsNullOrEmpty(label))
    {
      throw new InvalidOperationException(
        "Node labels must not be empty");
    }
    id ??= label;
    if(_knownNodes.Contains(id))
    {
      throw new InvalidOperationException(
        $"Duplicate node declaration '{id}'");
    }
    _knownNodes.Add(id);
    Writer.WriteLine($"  \"{id}\" ["); // Always quote, to err on the safe side
    if(!String.IsNullOrEmpty(shape))
    {
      Writer.WriteLine($"    shape={shape}");
    }
    Writer.Write("    label=<");
    Writer.Write($"{label}<BR ALIGN=\"LEFT\"/>");
    foreach(var sublabel in sublabels)
    {
      if(!String.IsNullOrEmpty(sublabel))
      {
        Writer.Write($"<I>{sublabel}</I><BR ALIGN=\"LEFT\"/>");
      }
    }
    Writer.WriteLine(">");
    if(!String.IsNullOrEmpty(style))
    {
      Writer.WriteLine($"    style={style}");
    }
    if(!String.IsNullOrEmpty(color))
    {
      Writer.WriteLine($"    fillcolor=\"{color}\"");
    }
    Writer.WriteLine("  ];");
  }

  /// <summary>
  /// Add an edge between two previously added nodes
  /// </summary>
  public void AddEdge(
    string nodeId1,
    string nodeId2,
    bool weightless = false,
    string? color = null)
  {
    if(!_knownNodes.Contains(nodeId1))
    {
      throw new ArgumentException(
        $"Edge source was not added as node yet: {nodeId1}");
    }
    if(!_knownNodes.Contains(nodeId2))
    {
      throw new ArgumentException(
        $"Edge target was not added as node yet: {nodeId2}");
    }
    var edgeop = Directed ? "->" : "--";
    Writer.WriteLine($"  \"{nodeId1}\" {edgeop} \"{nodeId2}\" [");
    if(weightless)
    {
      Writer.WriteLine("    weight=0");
    }
    if(!String.IsNullOrEmpty(color))
    {
      Writer.WriteLine($"    color=\"{color}\"");
    }
    Writer.WriteLine("  ];");
  }

  private TextWriter Writer {
    get {
      return
        _writer == null
        ? throw new ObjectDisposedException(nameof(Writer))
        : _writer;
    }
  }

  /// <summary>
  /// Clean up
  /// </summary>
  public void Dispose()
  {
    if(_writer != null)
    {
      Writer.WriteLine("}");
      _writer.Dispose();
      _writer = null;
    }
  }
}
