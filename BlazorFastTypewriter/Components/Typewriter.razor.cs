using System.Collections.Immutable;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorFastTypewriter;

/// <summary>
/// High-performance Blazor typewriter component for character-by-character text animation with HTML support.
/// Optimized for minimal allocations and maximum performance using modern .NET 10 features.
/// </summary>
public partial class Typewriter : ComponentBase, IAsyncDisposable
{
  private int _generation;
  private bool _isPaused;
  private bool _isRunning;
  private int _currentIndex;
  private ImmutableArray<NodeOperation> _operations = [];
  private RenderFragment? _originalContent;
  private CancellationTokenSource? _cancellationTokenSource;
  private ElementReference _containerRef;
  private IJSObjectReference? _jsModule;
  private bool _prefersReducedMotion;
  private bool _isInitialized;
  private readonly string _containerId = Guid.NewGuid().ToString("N")[..8];

  /// <summary>
  /// Gets or sets the JavaScript runtime for reduced motion detection and DOM parsing.
  /// </summary>
  [Inject]
  private IJSRuntime JSRuntime { get; set; } = null!;

  /// <summary>
  /// Gets or sets the content to be animated.
  /// </summary>
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  /// <summary>
  /// Gets or sets the typing speed in characters per second (default: 100).
  /// </summary>
  [Parameter]
  public int Speed { get; set; } = 100;

  /// <summary>
  /// Gets or sets the minimum animation duration in milliseconds (default: 50).
  /// </summary>
  [Parameter]
  public int MinDuration { get; set; } = 50;

  /// <summary>
  /// Gets or sets the maximum animation duration in milliseconds (default: 500).
  /// </summary>
  [Parameter]
  public int MaxDuration { get; set; } = 500;

  /// <summary>
  /// Gets or sets whether to auto-start the animation on load (default: true).
  /// </summary>
  [Parameter]
  public bool Autostart { get; set; } = true;

  /// <summary>
  /// Gets or sets the text direction: "ltr" or "rtl" (default: "ltr").
  /// </summary>
  [Parameter]
  public string Dir { get; set; } = "ltr";

  /// <summary>
  /// Gets or sets whether to respect the prefers-reduced-motion media query (default: false).
  /// </summary>
  [Parameter]
  public bool RespectMotionPreference { get; set; } = false;

  /// <summary>
  /// Gets or sets the ARIA label for the container region.
  /// </summary>
  [Parameter]
  public string? AriaLabel { get; set; }

  /// <summary>
  /// Event callback fired when animation starts.
  /// </summary>
  [Parameter]
  public EventCallback OnStart { get; set; }

  /// <summary>
  /// Event callback fired when animation pauses.
  /// </summary>
  [Parameter]
  public EventCallback OnPause { get; set; }

  /// <summary>
  /// Event callback fired when animation resumes.
  /// </summary>
  [Parameter]
  public EventCallback OnResume { get; set; }

  /// <summary>
  /// Event callback fired when animation completes.
  /// </summary>
  [Parameter]
  public EventCallback OnComplete { get; set; }

  /// <summary>
  /// Event callback fired when component resets.
  /// </summary>
  [Parameter]
  public EventCallback OnReset { get; set; }

  /// <summary>
  /// Event callback fired every 10 characters with progress information.
  /// </summary>
  [Parameter]
  public EventCallback<TypewriterProgressEventArgs> OnProgress { get; set; }

  /// <summary>
  /// Gets the current rendered content.
  /// </summary>
  private RenderFragment? CurrentContent { get; set; }

  /// <summary>
  /// Gets whether the component is currently running.
  /// </summary>
  public bool IsRunning => _isRunning;

  /// <summary>
  /// Gets whether the component is currently paused.
  /// </summary>
  public bool IsPaused => _isPaused;

  protected override void OnInitialized()
  {
    _originalContent = ChildContent;
    if (!Autostart && ChildContent is not null)
    {
      CurrentContent = ChildContent;
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      try
      {
        _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
          "import",
          "./_content/BlazorFastTypewriter/Components/Typewriter.razor.js"
        );

        if (RespectMotionPreference)
        {
          _prefersReducedMotion = await _jsModule.InvokeAsync<bool>("checkReducedMotion");
        }

        _isInitialized = true;

        if (Autostart && ChildContent is not null)
        {
          // Small delay to ensure DOM is ready for extraction
          await Task.Delay(100);
          await Start();
        }
      }
      catch (Exception)
      {
        // JavaScript not available (SSR scenario) or JS interop failed, show content immediately
        _isInitialized = true;
        if (ChildContent is not null && !Autostart)
        {
          CurrentContent = ChildContent;
          StateHasChanged();
        }
      }
    }

    await base.OnAfterRenderAsync(firstRender);
  }

  /// <summary>
  /// Begins the animation from the start.
  /// </summary>
  public async Task Start()
  {
    if (_isRunning || ChildContent is null)
      return;

    _generation++;
    _currentIndex = 0;
    _isPaused = false;

    // Capture original content if not already set
    if (_originalContent is null)
    {
      _originalContent = ChildContent;
    }

    // Extract DOM structure using JS interop
    if (_jsModule is not null && _isInitialized)
    {
      try
      {
        // First render the content so we can extract it
        CurrentContent = _originalContent;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(50); // Allow DOM to update

        var structure = await _jsModule.InvokeAsync<DomStructure>("extractStructure", _containerId);
        _operations = ParseDomStructure(structure);
      }
      catch (Exception)
      {
        // Fallback: show content immediately if extraction fails
        CurrentContent = _originalContent;
        await InvokeAsync(StateHasChanged);
        return;
      }
    }
    else
    {
      // No JS available, show content immediately
      CurrentContent = _originalContent;
      await InvokeAsync(StateHasChanged);
      return;
    }

    if (_prefersReducedMotion)
    {
      CurrentContent = _originalContent;
      _isRunning = false;
      await InvokeAsync(StateHasChanged);
      await OnComplete.InvokeAsync();
      return;
    }

    // Check if we have operations to animate
    if (_operations.Length == 0)
    {
      CurrentContent = _originalContent;
      await InvokeAsync(StateHasChanged);
      return;
    }

    var gen = _generation;
    _isRunning = true;
    _cancellationTokenSource?.Cancel();
    _cancellationTokenSource?.Dispose();
    _cancellationTokenSource = new CancellationTokenSource();
    var ct = _cancellationTokenSource.Token;

    // Clear content to start animation
    CurrentContent = null;
    await OnStart.InvokeAsync();
    await InvokeAsync(StateHasChanged);

    var totalChars = _operations.Count(static op => op.Type == OperationType.Char);
    var duration = Math.Max(
      MinDuration,
      Math.Min(MaxDuration, (int)Math.Round((totalChars / (double)Speed) * 1000))
    );
    var delay = totalChars > 0 ? Math.Max(8, duration / totalChars) : 0;

    _ = Task.Run(() => AnimateAsync(gen, delay, totalChars, ct), ct);
  }

  /// <summary>
  /// Pauses the current animation.
  /// </summary>
  public async Task Pause()
  {
    if (!_isRunning || _isPaused)
      return;

    _isPaused = true;
    await OnPause.InvokeAsync();
    await InvokeAsync(StateHasChanged);
  }

  /// <summary>
  /// Resumes a paused animation.
  /// </summary>
  public async Task Resume()
  {
    if (!_isPaused || !_isRunning)
      return;

    _isPaused = false;
    await OnResume.InvokeAsync();
    StateHasChanged();

    var gen = _generation;
    var totalChars = _operations.Count(static op => op.Type == OperationType.Char);
    var duration = Math.Max(
      MinDuration,
      Math.Min(MaxDuration, (int)Math.Round((totalChars / (double)Speed) * 1000))
    );
    var delay = totalChars > 0 ? Math.Max(8, duration / totalChars) : 0;

    _ = Task.Run(
      () =>
        AnimateAsync(
          gen,
          delay,
          totalChars,
          _cancellationTokenSource?.Token ?? CancellationToken.None
        )
    );
  }

  /// <summary>
  /// Completes the animation instantly, displaying all content.
  /// </summary>
  public async Task Complete()
  {
    if (!_isRunning)
      return;

    _generation++;
    _isRunning = false;
    _isPaused = false;
    _currentIndex = 0;

    CurrentContent = _originalContent;
    _cancellationTokenSource?.Cancel();

    await InvokeAsync(StateHasChanged);
    await OnComplete.InvokeAsync();
  }

  /// <summary>
  /// Resets the component, clearing content and state.
  /// </summary>
  public async Task Reset()
  {
    _generation++;
    _isRunning = false;
    _isPaused = false;
    _currentIndex = 0;
    _operations = [];
    CurrentContent = null;
    _cancellationTokenSource?.Cancel();

    await InvokeAsync(StateHasChanged);
    await OnProgress.InvokeAsync(new TypewriterProgressEventArgs(0, 0, 0));
    await OnReset.InvokeAsync();
  }

  /// <summary>
  /// Replaces the content and resets the component.
  /// </summary>
  public async Task SetText(RenderFragment newContent)
  {
    ChildContent = newContent;
    _originalContent = newContent;
    await Reset();
  }

  /// <summary>
  /// Replaces the content with HTML string and resets the component.
  /// </summary>
  public async Task SetText(string html)
  {
    ChildContent = builder => builder.AddMarkupContent(0, html);
    _originalContent = ChildContent;
    await Reset();
  }

  private async Task AnimateAsync(
    int generation,
    int baseDelay,
    int totalChars,
    CancellationToken cancellationToken
  )
  {
    var charCount = 0;
    var currentHtml = new StringBuilder(1024);

    for (var i = _currentIndex; i < _operations.Length; i++)
    {
      if (generation != _generation || !_isRunning || cancellationToken.IsCancellationRequested)
        return;

      if (_isPaused)
      {
        _currentIndex = i;
        // Use a longer delay when paused to reduce CPU usage
        await Task.Delay(100, cancellationToken);
        i--; // Retry same index
        continue;
      }

      var op = _operations[i];

      switch (op.Type)
      {
        case OperationType.OpenTag:
          currentHtml.Append(op.TagHtml);
          break;

        case OperationType.Char:
          currentHtml.Append(op.Char);
          charCount++;
          break;

        case OperationType.CloseTag:
          currentHtml.Append(op.TagHtml);
          break;
      }

      // Update content via InvokeAsync to ensure thread safety
      var html = currentHtml.ToString();
      await InvokeAsync(() =>
      {
        CurrentContent = builder => builder.AddMarkupContent(0, html);
        StateHasChanged();
      });

      if (op.Type == OperationType.Char && charCount % 10 == 0 && totalChars > 0)
      {
        await OnProgress.InvokeAsync(
          new TypewriterProgressEventArgs(
            charCount,
            totalChars,
            (charCount / (double)totalChars) * 100
          )
        );
      }

      if (op.Type == OperationType.Char)
      {
        var itemDelay = baseDelay + Random.Shared.Next(0, 6);
        if (itemDelay > 0)
        {
          await Task.Delay(itemDelay, cancellationToken);
        }
      }
    }

    _isRunning = false;
    await InvokeAsync(() =>
    {
      CurrentContent = _originalContent;
      StateHasChanged();
    });
    await OnComplete.InvokeAsync();
  }

  private ImmutableArray<NodeOperation> ParseDomStructure(DomStructure structure)
  {
    if (structure.Nodes is null or { Length: 0 })
      return [];

    var builder = ImmutableArray.CreateBuilder<NodeOperation>(initialCapacity: structure.Nodes.Length * 4);

    foreach (var node in structure.Nodes)
    {
      switch (node.Type)
      {
        case "element":
          if (node.TagName is not null)
          {
            // Build opening tag with attributes
            var openTag = BuildTag(node.TagName, node.Attributes, false);
            builder.Add(new NodeOperation(OperationType.OpenTag, TagHtml: openTag));

            // Process children recursively
            if (node.Children is not null)
            {
              foreach (var child in node.Children)
              {
                ProcessNode(child, builder);
              }
            }

            // Closing tag
            var closeTag = $"</{node.TagName}>";
            builder.Add(new NodeOperation(OperationType.CloseTag, TagHtml: closeTag));
          }
          break;

        case "text":
          if (node.Text is not null)
          {
            var normalized = System.Text.RegularExpressions.Regex.Replace(node.Text, @"\s+", " ");
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

    return builder.ToImmutable();
  }

  private static void ProcessNode(DomNode node, ImmutableArray<NodeOperation>.Builder builder)
  {
    switch (node.Type)
    {
      case "element":
        if (node.TagName is not null)
        {
          var openTag = BuildTag(node.TagName, node.Attributes, false);
          builder.Add(new NodeOperation(OperationType.OpenTag, TagHtml: openTag));

          if (node.Children is not null)
          {
            foreach (var child in node.Children)
            {
              ProcessNode(child, builder);
            }
          }

          var closeTag = $"</{node.TagName}>";
          builder.Add(new NodeOperation(OperationType.CloseTag, TagHtml: closeTag));
        }
        break;

      case "text":
        if (node.Text is not null)
        {
          var normalized = System.Text.RegularExpressions.Regex.Replace(node.Text, @"\s+", " ");
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

  public async ValueTask DisposeAsync()
  {
    _generation++; // Prevent any ongoing animations from continuing
    _isRunning = false;
    _cancellationTokenSource?.Cancel();
    _cancellationTokenSource?.Dispose();
    _cancellationTokenSource = null;

    if (_jsModule is not null)
    {
      try
      {
        await _jsModule.DisposeAsync();
      }
      catch (JSDisconnectedException)
      {
        // Ignore if JS context is disconnected
      }
      catch (ObjectDisposedException)
      {
        // Ignore if already disposed
      }
    }
  }

  private enum OperationType
  {
    OpenTag,
    Char,
    CloseTag
  }

  private sealed record NodeOperation(OperationType Type, char Char = default, string TagHtml = "");

  private sealed record DomStructure(DomNode[]? Nodes);

  private sealed record DomNode(
    string Type,
    string? TagName = null,
    Dictionary<string, string>? Attributes = null,
    string? Text = null,
    DomNode[]? Children = null
  );
}

/// <summary>
/// Event arguments for progress events.
/// </summary>
public sealed record TypewriterProgressEventArgs(int Current, int Total, double Percent);
