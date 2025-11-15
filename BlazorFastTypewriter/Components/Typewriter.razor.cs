using System.Collections.Immutable;
using BlazorFastTypewriter.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorFastTypewriter;

/// <summary>
/// High-performance typewriter component for character-by-character text animation with HTML support.
/// </summary>
public partial class Typewriter : ComponentBase, IAsyncDisposable
{
  private int _generation;
  private bool _isPaused;
  private bool _isRunning;
  private bool _isExtracting;
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

  [Inject]
  private IJSRuntime JSRuntime { get; set; } = null!;

  [Inject]
  private NavigationManager Navigation { get; set; } = null!;

  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  [Parameter]
  public int Speed { get; set; } = 100;

  [Parameter]
  public int MinDuration { get; set; } = 100;

  [Parameter]
  public int MaxDuration { get; set; } = 30000;

  [Parameter]
  public bool Autostart { get; set; } = true;

  [Parameter]
  public string Dir { get; set; } = "ltr";

  [Parameter]
  public bool RespectMotionPreference { get; set; }

  [Parameter]
  public string? AriaLabel { get; set; }

  [Parameter]
  public EventCallback OnStart { get; set; }

  [Parameter]
  public EventCallback OnPause { get; set; }

  [Parameter]
  public EventCallback OnResume { get; set; }

  [Parameter]
  public EventCallback OnComplete { get; set; }

  [Parameter]
  public EventCallback OnReset { get; set; }

  [Parameter]
  public EventCallback<TypewriterProgressEventArgs> OnProgress { get; set; }

  [Parameter]
  public EventCallback<TypewriterSeekEventArgs> OnSeek { get; set; }

  private RenderFragment? CurrentContent { get; set; }

  public bool IsRunning => _isRunning;

  public bool IsPaused => _isPaused;

  private bool ShouldShowChildContent() => CurrentContent is null && (!Autostart || _isInitialized);

  private string GetVisibilityStyle()
  {
    if (_isExtracting || 
        (_isRunning && CurrentContent is null) ||
        (Autostart && !_isInitialized))
      return "visibility: hidden;";
    
    return string.Empty;
  }

  protected override void OnInitialized()
  {
    _originalContent = ChildContent;
    CurrentContent = Autostart ? null : ChildContent;
  }

  protected override void OnParametersSet()
  {
    if (ChildContent != _originalContent && !_isRunning && !_isExtracting)
    {
      _originalContent = ChildContent;
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

    string? modulePath = null;
    try
    {
      var baseUri = Navigation.BaseUri.TrimEnd('/');
      modulePath = $"{baseUri}/_content/BlazorFastTypewriter/Components/Typewriter.razor.js";
      _jsModule = await JSRuntime
        .InvokeAsync<IJSObjectReference>("import", modulePath)
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
        await Task.Delay(100).ConfigureAwait(false);
        await Start().ConfigureAwait(false);
      }
    }
    catch (Exception)
    {
      #if DEBUG
      Console.Error.WriteLine($"Typewriter: Failed to load JavaScript module. Path: {modulePath ?? "unknown"}");
      #endif
      
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
    _generation++;
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
      catch (JSDisconnectedException) { }
      catch (ObjectDisposedException) { }
    }
  }
}
