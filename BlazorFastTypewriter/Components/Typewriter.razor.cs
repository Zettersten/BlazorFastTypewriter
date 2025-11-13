using System.Collections.Immutable;
using BlazorFastTypewriter.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorFastTypewriter;

/// <summary>
/// High-performance Blazor typewriter component for character-by-character text animation with HTML support.
/// Optimized for minimal allocations and maximum performance using modern .NET 10 features.
/// </summary>
public partial class Typewriter : ComponentBase, IAsyncDisposable
{
  // Private fields - optimized with modern patterns
  private int _generation;

  private bool _isPaused;
  private bool _isRunning;
  private bool _isExtracting; // Flag to track DOM extraction phase
  private int _currentIndex;
  private int _totalChars;
  private int _currentCharCount;
  private ImmutableArray<NodeOperation> _operations = [];
  private RenderFragment? _originalContent;
  private CancellationTokenSource? _cancellationTokenSource;
  private ElementReference _containerRef;
  private IJSObjectReference? _jsModule;
  private bool _prefersReducedMotion;
  private bool _isInitialized;
  private readonly string _containerId = Guid.CreateVersion7().ToString("N")[..8];
  private readonly Lock _animationLock = new();
  private readonly DomParsingService _domParser = new();

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
  /// Gets or sets the minimum animation duration in milliseconds (default: 100).
  /// </summary>
  [Parameter]
  public int MinDuration { get; set; } = 100;

  /// <summary>
  /// Gets or sets the maximum animation duration in milliseconds (default: 30000).
  /// </summary>
  [Parameter]
  public int MaxDuration { get; set; } = 30000;

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
  /// Event callback fired when seeking to a new position.
  /// </summary>
  [Parameter]
  public EventCallback<TypewriterSeekEventArgs> OnSeek { get; set; }

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

  /// <summary>
  /// Determines whether to show ChildContent fallback.
  /// Hides content when Autostart is enabled and component hasn't initialized yet to prevent flash.
  /// </summary>
  private bool ShouldShowChildContent() => CurrentContent is null && (!Autostart || _isInitialized);

  /// <summary>
  /// Gets visibility style to hide content flash before animation.
  /// </summary>
  private string GetVisibilityStyle()
  {
    // Hide content in these scenarios:
    // 1. During extraction phase
    // 2. When running but content hasn't started animating
    // 3. When Autostart is enabled but not yet initialized (prevents initial flash)
    if (_isExtracting || 
        (_isRunning && CurrentContent is null) ||
        (Autostart && !_isInitialized))
      return "visibility: hidden;";
    
    return string.Empty;
  }

  protected override void OnInitialized()
  {
    _originalContent = ChildContent;
    // For Autostart, hide content until animation begins
    // For manual start, show content immediately
    CurrentContent = Autostart ? null : ChildContent;
  }

  protected override void OnParametersSet()
  {
    // CRITICAL: Update _originalContent when ChildContent parameter changes
    // This ensures dynamic content updates (like AI chat) use the correct content
    if (ChildContent != _originalContent && !_isRunning && !_isExtracting)
    {
      _originalContent = ChildContent;
      // Don't update CurrentContent if Autostart - let the animation control it
      if (!Autostart || _isInitialized)
      {
        CurrentContent = ChildContent;
      }
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (!firstRender)
      return;

    try
    {
      _jsModule = await JSRuntime
        .InvokeAsync<IJSObjectReference>(
          "import",
          "./_content/BlazorFastTypewriter/Components/Typewriter.razor.js"
        )
        .ConfigureAwait(false);

      if (RespectMotionPreference)
      {
        _prefersReducedMotion = await _jsModule
          .InvokeAsync<bool>("checkReducedMotion")
          .ConfigureAwait(false);
      }

      _isInitialized = true;

      if (Autostart && ChildContent is not null)
      {
        // Small delay to ensure DOM is ready for extraction
        await Task.Delay(100).ConfigureAwait(false);
        await Start().ConfigureAwait(false);
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
}
