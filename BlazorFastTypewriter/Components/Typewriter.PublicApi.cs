using System.Collections.Immutable;
using BlazorFastTypewriter.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter;

/// <summary>
/// Public API methods for the Typewriter component.
/// </summary>
public partial class Typewriter
{
  /// <summary>
  /// Begins the animation from the start.
  /// </summary>
  public async Task Start()
  {
    if (ChildContent is null)
      return;

    // If paused (e.g., from seek), just resume instead of restarting
    // Do this BEFORE acquiring lock to avoid lock issues
    if (_isRunning && _isPaused)
    {
      await Resume().ConfigureAwait(false);
      return;
    }

    // Thread-safe lock to prevent multiple simultaneous starts
    lock (_animationLock)
    {
      // If already running and not paused, don't restart
      if (_isRunning)
        return;

      _generation++;
      _currentIndex = 0;
      _currentCharCount = 0;
      _isPaused = false;

      // Capture original content if not already set
      _originalContent ??= ChildContent;
    }

    // Extract DOM structure using JS interop
    if (_jsModule is not null && _isInitialized)
    {
      try
      {
        // CRITICAL: Set extraction flag and render content in hidden extraction container
        _isExtracting = true;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        
        // Wait for Blazor to fully render the content in the extraction container
        await Task.Delay(250).ConfigureAwait(false);

        // Extract from the hidden extraction container
        var structure = await _jsModule
          .InvokeAsync<DomStructure>("extractStructure", [$"{_containerId}-extract"])
          .ConfigureAwait(false);

        _operations = _domParser.ParseDomStructure(structure);
        _totalChars = _operations.Count(static op => op.Type == OperationType.Char);
        
        // Clear extraction flag
        _isExtracting = false;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
      }
      catch (Exception)
      {
        // Fallback: Create simple text-based operations without DOM parsing
        _operations = [];
        _totalChars = 0;
        _isExtracting = false;
      }

      // If operations are still empty after parsing, skip animation and just show content
      if (_operations.Length == 0)
      {
        // No valid operations - just show the content immediately
        _isRunning = true;
        await OnStart.InvokeAsync().ConfigureAwait(false);
        _isRunning = false;
        CurrentContent = _originalContent;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        await OnComplete.InvokeAsync().ConfigureAwait(false);
        return;
      }
    }
    else
    {
      // No JS available - skip animation and show content
      CurrentContent = _originalContent;
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);
      await OnStart.InvokeAsync().ConfigureAwait(false);
      return;
    }

    if (_prefersReducedMotion)
    {
      _isRunning = true;
      await OnStart.InvokeAsync().ConfigureAwait(false);
      _isRunning = false;
      CurrentContent = _originalContent;
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);
      await OnComplete.InvokeAsync().ConfigureAwait(false);
      return;
    }

    var gen = _generation;
    _isRunning = true;
    _cancellationTokenSource?.Cancel();
    _cancellationTokenSource?.Dispose();
    _cancellationTokenSource = new CancellationTokenSource();
    var ct = _cancellationTokenSource.Token;

    await OnStart.InvokeAsync().ConfigureAwait(false);

    // Clear content to start animation
    CurrentContent = null;
    await InvokeAsync(StateHasChanged).ConfigureAwait(false);

    var duration = Math.Max(
      MinDuration,
      Math.Min(MaxDuration, (int)Math.Round((_totalChars / (double)Speed) * 1000))
    );
    var delay = _totalChars > 0 ? Math.Max(8, duration / _totalChars) : 0;

    // Run animation in background with error handling
    _ = Task.Run(
      async () =>
      {
        try
        {
          await AnimateAsync(gen, delay, _totalChars, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
          // Task was cancelled, this is expected - do nothing
        }
        catch (Exception)
        {
          // On error, ensure content is restored
          _isRunning = false;
          await InvokeAsync(() =>
          {
            CurrentContent = _originalContent;
            StateHasChanged();
          }).ConfigureAwait(false);
          await OnComplete.InvokeAsync().ConfigureAwait(false);
        }
      },
      ct
    );
  }

  /// <summary>
  /// Pauses the current animation.
  /// </summary>
  public async Task Pause()
  {
    if (!_isRunning || _isPaused)
      return;

    // Thread-safe lock for pause operation
    lock (_animationLock)
    {
      if (_isPaused) // Double-check after acquiring lock
        return;

      _isPaused = true;
    }

    await OnPause.InvokeAsync().ConfigureAwait(false);
    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
  }

  /// <summary>
  /// Resumes a paused animation.
  /// </summary>
  public async Task Resume()
  {
    if (!_isPaused || !_isRunning)
      return;

    int gen;
    CancellationToken ct;

    // Thread-safe lock to prevent race conditions
    lock (_animationLock)
    {
      if (!_isPaused || !_isRunning) // Double-check after acquiring lock
        return;

      // Increment generation to invalidate any old paused tasks
      _generation++;
      gen = _generation;

      // Cancel and recreate cancellation token to stop old tasks immediately
      _cancellationTokenSource?.Cancel();
      _cancellationTokenSource?.Dispose();
      _cancellationTokenSource = new CancellationTokenSource();
      ct = _cancellationTokenSource.Token;

      // Now set isPaused to false AFTER cancelling old tasks
      _isPaused = false;
    }

    await OnResume.InvokeAsync().ConfigureAwait(false);
    await InvokeAsync(StateHasChanged).ConfigureAwait(false);

    // Start animation task from current position (handles seek scenario)
    var duration = Math.Max(
      MinDuration,
      Math.Min(MaxDuration, (int)Math.Round((_totalChars / (double)Speed) * 1000))
    );
    var delay = _totalChars > 0 ? Math.Max(8, duration / _totalChars) : 0;

    // Run animation with error handling
    _ = Task.Run(
      async () =>
      {
        try
        {
          await AnimateAsync(gen, delay, _totalChars, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
          // Task was cancelled, this is expected - do nothing
        }
        catch (Exception)
        {
          // On unexpected error, ensure content is restored
          _isRunning = false;
          await InvokeAsync(() =>
          {
            CurrentContent = _originalContent;
            StateHasChanged();
          }).ConfigureAwait(false);
          await OnComplete.InvokeAsync().ConfigureAwait(false);
        }
      },
      ct
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
    _currentCharCount = _totalChars;

    CurrentContent = _originalContent;
    _cancellationTokenSource?.Cancel();

    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    await OnComplete.InvokeAsync().ConfigureAwait(false);
  }

  /// <summary>
  /// Resets the component, clearing content and state.
  /// </summary>
  public async Task Reset()
  {
    _generation++;
    _isRunning = false;
    _isPaused = false;
    _isExtracting = false;
    _currentIndex = 0;
    _currentCharCount = 0;
    _totalChars = 0;
    _operations = [];
    CurrentContent = null;
    _cancellationTokenSource?.Cancel();

    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    await OnProgress.InvokeAsync(new TypewriterProgressEventArgs(0, 0, 0)).ConfigureAwait(false);
    await OnReset.InvokeAsync().ConfigureAwait(false);
  }

  /// <summary>
  /// Replaces the content and resets the component.
  /// </summary>
  public async Task SetText(RenderFragment newContent)
  {
    ChildContent = newContent;
    _originalContent = newContent;
    await Reset().ConfigureAwait(false);
  }

  /// <summary>
  /// Replaces the content with HTML string and resets the component.
  /// </summary>
  public async Task SetText(string html)
  {
    ChildContent = builder => builder.AddMarkupContent(0, html);
    _originalContent = ChildContent;
    await Reset().ConfigureAwait(false);
  }

  /// <summary>
  /// Seeks to a specific position in the animation (0.0 to 1.0).
  /// </summary>
  /// <param name="position">Position from 0.0 (start) to 1.0 (end).</param>
  public async Task Seek(double position)
  {
    if (_originalContent is null)
      return;

    // Ensure operations are built
    if (_operations.Length == 0)
    {
      await RebuildFromOriginal().ConfigureAwait(false);
    }

    // Normalize position
    var normalizedPosition = Math.Clamp(position, 0, 1);

    // Remember if animation was running
    var wasRunning = _isRunning && !_isPaused;

    // CRITICAL: Increment generation and cancel old tasks BEFORE pausing
    // This prevents old paused tasks from overwriting _currentIndex
    _generation++;
    _cancellationTokenSource?.Cancel();
    _cancellationTokenSource?.Dispose();
    _cancellationTokenSource = new CancellationTokenSource();

    // Pause if running, or set paused state if not running
    if (wasRunning)
    {
      // Set paused state directly (don't call Pause() as it tries to acquire lock)
      _isPaused = true;
    }
    else if (!_isRunning)
    {
      _isRunning = true;
      _isPaused = true;
    }

    // Calculate target character
    var targetChar = (int)(normalizedPosition * _totalChars);

    // Build DOM to target - now safe as old tasks are cancelled
    await BuildDOMToIndex(targetChar).ConfigureAwait(false);

    // Handle edge cases based on normalized position (not current char count)
    var atStart = normalizedPosition == 0;
    var atEnd = normalizedPosition >= 1.0;

    if (atStart || atEnd)
    {
      _isRunning = false;
      _isPaused = false;
    }

    // Fire seek event
    await OnSeek.InvokeAsync(
      new TypewriterSeekEventArgs(
        Position: normalizedPosition,
        TargetChar: _currentCharCount,
        TotalChars: _totalChars,
        Percent: _totalChars > 0 ? (_currentCharCount / (double)_totalChars) * 100 : 0,
        WasRunning: wasRunning,
        CanResume: !atStart && !atEnd,
        AtStart: atStart,
        AtEnd: atEnd
      )
    ).ConfigureAwait(false);

    // Fire progress event
    await OnProgress.InvokeAsync(
      new TypewriterProgressEventArgs(
        _currentCharCount,
        _totalChars,
        _totalChars > 0 ? (_currentCharCount / (double)_totalChars) * 100 : 0
      )
    ).ConfigureAwait(false);

    // If at end, fire complete event
    if (atEnd)
    {
      await OnComplete.InvokeAsync().ConfigureAwait(false);
    }
  }

  /// <summary>
  /// Seeks to a specific percentage (0 to 100).
  /// </summary>
  /// <param name="percent">Percentage from 0 to 100.</param>
  public Task SeekToPercent(double percent) => Seek(percent / 100);

  /// <summary>
  /// Seeks to a specific character index.
  /// </summary>
  /// <param name="charIndex">Character index to seek to.</param>
  public Task SeekToChar(int charIndex) =>
    _totalChars == 0 ? Task.CompletedTask : Seek(charIndex / (double)_totalChars);

  /// <summary>
  /// Gets the current progress information.
  /// </summary>
  /// <returns>Current progress state.</returns>
  public TypewriterProgressInfo GetProgress() =>
    new(
      Current: _currentCharCount,
      Total: _totalChars,
      Percent: _totalChars > 0 ? (_currentCharCount / (double)_totalChars) * 100 : 0,
      Position: _totalChars > 0 ? _currentCharCount / (double)_totalChars : 0
    );
}
