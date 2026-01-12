namespace BlazorFastTypewriter;

public partial class Typewriter
{
  public async Task Start()
  {
    if (ChildContent is null)
      return;

    bool shouldResume = false;
    int resumeGen = 0;
    CancellationToken resumeCt = default;
    int resumeTotalChars = 0;
    int resumeDelay = 0;

    lock (_animationLock)
    {
      if (_isRunning && _isPaused)
      {
        _isPaused = false;
        _generation++;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        resumeGen = _generation;
        resumeCt = _cancellationTokenSource.Token;
        resumeTotalChars = _totalChars;
        resumeDelay = ComputeDelayMs(resumeTotalChars);
        shouldResume = true;
      }
      else if (_isRunning)
      {
        return;
      }
      else
      {
        _generation++;
        _currentIndex = 0;
        _currentCharCount = 0;
        _isPaused = false;
        _originalContent ??= ChildContent;
      }
    }

    if (shouldResume)
    {
      _ = Task.Run(
        async () =>
        {
          try
          {
            await AnimateAsync(resumeGen, resumeDelay, resumeTotalChars, resumeCt).ConfigureAwait(false);
          }
          catch (OperationCanceledException) { }
          catch (Exception)
          {
            lock (_animationLock)
            {
              _isRunning = false;
            }
            await InvokeAsync(ShowOriginalContent).ConfigureAwait(false);
            await OnComplete.InvokeAsync().ConfigureAwait(false);
          }
        },
        resumeCt
      );

      await OnResume.InvokeAsync().ConfigureAwait(false);
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);
      return;
    }

    if (_jsModule is not null && _isInitialized)
    {
      try
      {
        _isExtracting = true;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

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

        DomStructure? structure = null;
        const int maxRetries = 3;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
          structure = await _jsModule
            .InvokeAsync<DomStructure>("extractStructure", [$"{_containerId}-extract"])
            .ConfigureAwait(false);

          _operations = DomParsingService.ParseDomStructure(structure);
          _totalChars = CountChars(_operations);

          if (_operations.Length > 0 && _totalChars > 0)
            break;

          if (attempt < maxRetries - 1)
          {
            await Task.Delay(100 * (attempt + 1)).ConfigureAwait(false);
          }
        }

        ImmutableArray<NodeOperation> extractedOperations;
        int extractedTotalChars;
        lock (_animationLock)
        {
          _isExtracting = false;
          extractedOperations = _operations;
          extractedTotalChars = _totalChars;
        }
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        if (extractedOperations.Length == 0 || extractedTotalChars == 0)
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

        lock (_animationLock)
        {
          _operations = [];
          _totalChars = 0;
          _isExtracting = false;
        }
        
        lock (_animationLock)
        {
          _isRunning = true;
        }
        await OnStart.InvokeAsync().ConfigureAwait(false);
        lock (_animationLock)
        {
          _isRunning = false;
        }
        await InvokeAsync(ShowOriginalContent).ConfigureAwait(false);
        await OnComplete.InvokeAsync().ConfigureAwait(false);
        return;
      }
    }
    else
    {
      await InvokeAsync(ShowOriginalContent).ConfigureAwait(false);
      await OnStart.InvokeAsync().ConfigureAwait(false);
      return;
    }

    if (_prefersReducedMotion)
    {
      lock (_animationLock)
      {
        _isRunning = true;
      }
      await OnStart.InvokeAsync().ConfigureAwait(false);
      lock (_animationLock)
      {
        _isRunning = false;
      }
      CurrentContent = _originalContent;
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);
      await OnComplete.InvokeAsync().ConfigureAwait(false);
      return;
    }

    int gen;
    int totalChars;
    CancellationToken ct;
    lock (_animationLock)
    {
      gen = _generation;
      _isRunning = true;
      totalChars = _totalChars;
      _cancellationTokenSource?.Cancel();
      _cancellationTokenSource?.Dispose();
      _cancellationTokenSource = new CancellationTokenSource();
      ct = _cancellationTokenSource.Token;
    }

    await OnStart.InvokeAsync().ConfigureAwait(false);

    CurrentContent = null;
    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    var delay = ComputeDelayMs(totalChars);

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
          lock (_animationLock)
          {
            _isRunning = false;
          }
          await InvokeAsync(ShowOriginalContent).ConfigureAwait(false);
          await OnComplete.InvokeAsync().ConfigureAwait(false);
        }
      },
      ct
    );
  }

  public async Task Pause()
  {
    lock (_animationLock)
    {
      if (!_isRunning || _isPaused)
        return;

      _isPaused = true;
    }

    await OnPause.InvokeAsync().ConfigureAwait(false);
    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
  }

  public async Task Resume()
  {
    int resumeGen;
    int resumeTotalChars;
    CancellationToken resumeCt;

    lock (_animationLock)
    {
      if (!_isPaused || !_isRunning)
        return;

      _generation++;
      resumeGen = _generation;
      resumeTotalChars = _totalChars;

      _cancellationTokenSource?.Cancel();
      _cancellationTokenSource?.Dispose();
      _cancellationTokenSource = new CancellationTokenSource();
      resumeCt = _cancellationTokenSource.Token;

      _isPaused = false;
    }

    await OnResume.InvokeAsync().ConfigureAwait(false);
    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    var resumeDelay = ComputeDelayMs(resumeTotalChars);

    _ = Task.Run(
      async () =>
      {
        try
        {
          await AnimateAsync(resumeGen, resumeDelay, resumeTotalChars, resumeCt).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
          lock (_animationLock)
          {
            _isRunning = false;
          }
          await InvokeAsync(ShowOriginalContent).ConfigureAwait(false);
          await OnComplete.InvokeAsync().ConfigureAwait(false);
        }
      },
      resumeCt
    );
  }

  public async Task Complete()
  {
    lock (_animationLock)
    {
      if (!_isRunning)
        return;

      _generation++;
      _isRunning = false;
      _isPaused = false;
      _currentIndex = 0;
      _currentCharCount = _totalChars;
      _cancellationTokenSource?.Cancel();
    }

    CurrentContent = _originalContent;
    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    await OnComplete.InvokeAsync().ConfigureAwait(false);
  }

  public async Task Reset()
  {
    lock (_animationLock)
    {
      _generation++;
      _isRunning = false;
      _isPaused = false;
      _isExtracting = false;
      _currentIndex = 0;
      _currentCharCount = 0;
      _totalChars = 0;
      _operations = [];
      _cancellationTokenSource?.Cancel();
    }

    CurrentContent = null;
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
}

