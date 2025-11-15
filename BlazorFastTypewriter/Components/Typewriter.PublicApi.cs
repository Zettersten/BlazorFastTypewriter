using System.Collections.Immutable;
using BlazorFastTypewriter.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter;

public partial class Typewriter
{
  public async Task Start()
  {
    if (ChildContent is null)
      return;

    if (_isRunning && _isPaused)
    {
      await Resume().ConfigureAwait(false);
      return;
    }

    lock (_animationLock)
    {
      if (_isRunning)
        return;

      _generation++;
      _currentIndex = 0;
      _currentCharCount = 0;
      _isPaused = false;
      _originalContent ??= ChildContent;
    }

    if (_jsModule is not null && _isInitialized)
    {
      try
      {
        _isExtracting = true;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        await Task.Delay(150).ConfigureAwait(false);

        var elementReady = await _jsModule
          .InvokeAsync<bool>("waitForElement", [$"{_containerId}-extract", 3000])
          .ConfigureAwait(false);

        if (!elementReady)
        {
          _operations = [];
          _totalChars = 0;
          _isExtracting = false;
          throw new InvalidOperationException("Extraction container not found in DOM");
        }

        await Task.Delay(100).ConfigureAwait(false);

        DomStructure? structure = null;
        const int maxRetries = 3;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
          structure = await _jsModule
            .InvokeAsync<DomStructure>("extractStructure", [$"{_containerId}-extract"])
            .ConfigureAwait(false);

          _operations = DomParsingService.ParseDomStructure(structure);
          _totalChars = _operations.Count(static op => op.Type == OperationType.Char);

          if (_operations.Length > 0 && _totalChars > 0)
            break;

          if (attempt < maxRetries - 1)
          {
            await Task.Delay(100 * (attempt + 1)).ConfigureAwait(false);
          }
        }

        _isExtracting = false;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        if (_operations.Length == 0 || _totalChars == 0)
        {
          #if DEBUG
          Console.Error.WriteLine($"Typewriter: DOM extraction returned empty structure. Container ID: {_containerId}-extract");
          #endif
          throw new InvalidOperationException("DOM extraction returned empty structure");
        }
      }
      catch (Exception)
      {
        #if DEBUG
        Console.Error.WriteLine($"Typewriter: DOM extraction failed");
        #endif
        
        _operations = [];
        _totalChars = 0;
        _isExtracting = false;
        
        if (_operations.Length == 0)
        {
          _isRunning = true;
          await OnStart.InvokeAsync().ConfigureAwait(false);
          _isRunning = false;
          CurrentContent = _originalContent;
          await InvokeAsync(StateHasChanged).ConfigureAwait(false);
          await OnComplete.InvokeAsync().ConfigureAwait(false);
          return;
        }
      }
    }
    else
    {
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

    CurrentContent = null;
    await InvokeAsync(StateHasChanged).ConfigureAwait(false);

    var duration = Math.Max(
      MinDuration,
      Math.Min(MaxDuration, (int)Math.Round((_totalChars / (double)Speed) * 1000))
    );
    var delay = _totalChars > 0 ? Math.Max(8, duration / _totalChars) : 0;

    _ = Task.Run(
      async () =>
      {
        try
        {
          await AnimateAsync(gen, delay, _totalChars, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
          _isRunning = false;
          await InvokeAsync(() =>
            {
              CurrentContent = _originalContent;
              StateHasChanged();
            })
            .ConfigureAwait(false);
          await OnComplete.InvokeAsync().ConfigureAwait(false);
        }
      },
      ct
    );
  }

  public async Task Pause()
  {
    if (!_isRunning || _isPaused)
      return;

    lock (_animationLock)
    {
      if (_isPaused)
        return;

      _isPaused = true;
    }

    await OnPause.InvokeAsync().ConfigureAwait(false);
    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
  }

  public async Task Resume()
  {
    if (!_isPaused || !_isRunning)
      return;

    int gen;
    CancellationToken ct;

    lock (_animationLock)
    {
      if (!_isPaused || !_isRunning)
        return;

      _generation++;
      gen = _generation;

      _cancellationTokenSource?.Cancel();
      _cancellationTokenSource?.Dispose();
      _cancellationTokenSource = new CancellationTokenSource();
      ct = _cancellationTokenSource.Token;

      _isPaused = false;
    }

    await OnResume.InvokeAsync().ConfigureAwait(false);
    await InvokeAsync(StateHasChanged).ConfigureAwait(false);

    var duration = Math.Max(
      MinDuration,
      Math.Min(MaxDuration, (int)Math.Round((_totalChars / (double)Speed) * 1000))
    );
    var delay = _totalChars > 0 ? Math.Max(8, duration / _totalChars) : 0;

    _ = Task.Run(
      async () =>
      {
        try
        {
          await AnimateAsync(gen, delay, _totalChars, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
          _isRunning = false;
          await InvokeAsync(() =>
            {
              CurrentContent = _originalContent;
              StateHasChanged();
            })
            .ConfigureAwait(false);
          await OnComplete.InvokeAsync().ConfigureAwait(false);
        }
      },
      ct
    );
  }

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

  public async Task SetText(RenderFragment newContent)
  {
    ChildContent = newContent;
    _originalContent = newContent;
    await Reset().ConfigureAwait(false);
  }

  public async Task SetText(string html)
  {
    ChildContent = builder => builder.AddMarkupContent(0, html);
    _originalContent = ChildContent;
    await Reset().ConfigureAwait(false);
  }

  public async Task Seek(double position)
  {
    if (_originalContent is null)
      return;

    if (_operations.Length == 0)
    {
      await RebuildFromOriginal().ConfigureAwait(false);
    }

    var normalizedPosition = Math.Clamp(position, 0, 1);
    var wasRunning = _isRunning && !_isPaused;

    _generation++;
    _cancellationTokenSource?.Cancel();
    _cancellationTokenSource?.Dispose();
    _cancellationTokenSource = new CancellationTokenSource();

    if (wasRunning)
    {
      _isPaused = true;
    }
    else if (!_isRunning)
    {
      _isRunning = true;
      _isPaused = true;
    }

    var targetChar = (int)(normalizedPosition * _totalChars);
    await BuildDOMToIndex(targetChar).ConfigureAwait(false);

    var atStart = normalizedPosition == 0;
    var atEnd = normalizedPosition >= 1.0;

    if (atStart || atEnd)
    {
      _isRunning = false;
      _isPaused = false;
    }

    await OnSeek
      .InvokeAsync(
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
      )
      .ConfigureAwait(false);

    await OnProgress
      .InvokeAsync(
        new TypewriterProgressEventArgs(
          _currentCharCount,
          _totalChars,
          _totalChars > 0 ? (_currentCharCount / (double)_totalChars) * 100 : 0
        )
      )
      .ConfigureAwait(false);

    if (atEnd)
    {
      await OnComplete.InvokeAsync().ConfigureAwait(false);
    }
  }

  public Task SeekToPercent(double percent) => Seek(percent / 100);

  public Task SeekToChar(int charIndex) =>
    _totalChars == 0 ? Task.CompletedTask : Seek(charIndex / (double)_totalChars);

  public TypewriterProgressInfo GetProgress() =>
    new(
      Current: _currentCharCount,
      Total: _totalChars,
      Percent: _totalChars > 0 ? (_currentCharCount / (double)_totalChars) * 100 : 0,
      Position: _totalChars > 0 ? _currentCharCount / (double)_totalChars : 0
    );
}
