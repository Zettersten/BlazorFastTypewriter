using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace BlazorFastTypewriter.Services;

internal sealed partial class DomParsingService
{
  [GeneratedRegex(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
  private static partial Regex WhitespaceRegex();

  public static ImmutableArray<NodeOperation> ParseDomStructure(DomStructure structure)
  {
    if (structure.nodes is not { Length: > 0 })
      return [];

    var builder = ImmutableArray.CreateBuilder<NodeOperation>(
      initialCapacity: structure.nodes.Length * 4
    );

    foreach (var node in structure.nodes)
    {
      ProcessNode(node, builder);
    }

    return builder.DrainToImmutable();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static void ProcessNode(DomNode node, ImmutableArray<NodeOperation>.Builder builder)
  {
    switch (node.type)
    {
      case "element" when node.tagName is not null:
        ProcessElement(node, builder);
        break;

      case "text" when node.text is not null:
        ProcessTextNode(node.text, builder);
        break;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static void ProcessElement(DomNode node, ImmutableArray<NodeOperation>.Builder builder)
  {
    var openTag = BuildTag(node.tagName!, node.attributes, selfClosing: false);
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static void ProcessTextNode(string text, ImmutableArray<NodeOperation>.Builder builder)
  {
    var normalized = WhitespaceRegex().Replace(text, " ");
    if (string.IsNullOrWhiteSpace(normalized))
      return;

    // Use span-based iteration for better performance
    foreach (var ch in normalized.AsSpan())
    {
      builder.Add(new NodeOperation(OperationType.Char, Char: ch));
    }
  }

  private static string BuildTag(
    string tagName,
    Dictionary<string, string>? attributes,
    bool selfClosing
  )
  {
    if (attributes is null or { Count: 0 })
      return selfClosing ? $"<{tagName} />" : $"<{tagName}>";

    var estimatedLength = tagName.Length + (attributes.Count * 20) + 10;
    var buffer = ArrayPool<char>.Shared.Rent(estimatedLength);

    try
    {
      var span = buffer.AsSpan();
      var pos = 0;

      span[pos++] = '<';
      tagName.AsSpan().CopyTo(span[pos..]);
      pos += tagName.Length;

      foreach (var (key, value) in attributes)
      {
        span[pos++] = ' ';
        key.AsSpan().CopyTo(span[pos..]);
        pos += key.Length;

        if (!string.IsNullOrEmpty(value))
        {
          span[pos++] = '=';
          span[pos++] = '"';

          var encodedValue = System.Net.WebUtility.HtmlEncode(value);
          encodedValue.AsSpan().CopyTo(span[pos..]);
          pos += encodedValue.Length;

          span[pos++] = '"';
        }
      }

      if (selfClosing)
      {
        span[pos++] = ' ';
        span[pos++] = '/';
      }

      span[pos++] = '>';

      return new string(span[..pos]);
    }
    finally
    {
      ArrayPool<char>.Shared.Return(buffer);
    }
  }
}

internal enum OperationType : byte
{
  OpenTag,
  Char,
  CloseTag
}

internal readonly record struct NodeOperation(
  OperationType Type,
  char Char = default,
  string TagHtml = ""
);

internal sealed record DomStructure(DomNode[]? nodes);

internal sealed record DomNode(
  string type,
  string? tagName = null,
  Dictionary<string, string>? attributes = null,
  string? text = null,
  DomNode[]? children = null
);
