using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorFastTypewriter.Services;

/// <summary>
/// Service for parsing DOM structures into animation operations.
/// </summary>
internal sealed class DomParsingService
{
  private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

  /// <summary>
  /// Parses a DOM structure into an immutable array of operations.
  /// </summary>
  public ImmutableArray<NodeOperation> ParseDomStructure(DomStructure structure)
  {
    if (structure.nodes is null or { Length: 0 })
      return [];

    var builder = ImmutableArray.CreateBuilder<NodeOperation>(
      initialCapacity: structure.nodes.Length * 4
    );

    foreach (var node in structure.nodes)
    {
      ProcessNode(node, builder);
    }

    return builder.ToImmutable();
  }

  private static void ProcessNode(DomNode node, ImmutableArray<NodeOperation>.Builder builder)
  {
    switch (node.type)
    {
      case "element":
        if (node.tagName is not null)
        {
          var openTag = BuildTag(node.tagName, node.attributes, false);
          builder.Add(new NodeOperation(OperationType.OpenTag, TagHtml: openTag));

          if (node.children is not null)
          {
            foreach (var child in node.children)
            {
              ProcessNode(child, builder);
            }
          }

          var closeTag = $"</{node.tagName}>";
          builder.Add(new NodeOperation(OperationType.CloseTag, TagHtml: closeTag));
        }
        break;

      case "text":
        if (node.text is not null)
        {
          var normalized = WhitespaceRegex.Replace(node.text, " ");
          if (!string.IsNullOrWhiteSpace(normalized))
          {
            foreach (var ch in normalized)
            {
              builder.Add(new NodeOperation(OperationType.Char, Char: ch));
            }
          }
        }
        break;
    }
  }

  private static string BuildTag(
    string tagName,
    Dictionary<string, string>? attributes,
    bool selfClosing
  )
  {
    var sb = new StringBuilder(tagName.Length + (attributes?.Count * 20 ?? 0) + 10);
    sb.Append('<');
    sb.Append(tagName);

    if (attributes is not null)
    {
      foreach (var (key, value) in attributes)
      {
        sb.Append(' ');
        sb.Append(key);
        if (!string.IsNullOrEmpty(value))
        {
          sb.Append("=\"");
          sb.Append(System.Net.WebUtility.HtmlEncode(value));
          sb.Append('"');
        }
      }
    }

    if (selfClosing)
      sb.Append(" /");
    sb.Append('>');

    return sb.ToString();
  }
}

/// <summary>
/// Type of operation for animation.
/// </summary>
internal enum OperationType
{
  OpenTag,
  Char,
  CloseTag
}

/// <summary>
/// Represents a single operation in the animation sequence.
/// </summary>
internal sealed record NodeOperation(OperationType Type, char Char = default, string TagHtml = "");

/// <summary>
/// Represents a DOM structure from JavaScript interop.
/// </summary>
internal sealed record DomStructure(DomNode[]? nodes);

/// <summary>
/// Represents a single DOM node from JavaScript interop.
/// </summary>
internal sealed record DomNode(
  string type,
  string? tagName = null,
  Dictionary<string, string>? attributes = null,
  string? text = null,
  DomNode[]? children = null
);
