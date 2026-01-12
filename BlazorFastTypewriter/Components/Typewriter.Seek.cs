namespace BlazorFastTypewriter;

public partial class Typewriter
{
  public async Task Seek(double position)
  {
    if (_originalContent is null)
      return;

    bool wasRunning;
    int totalChars;
    ImmutableArray<NodeOperation> operations;

    lock (_animationLock)
    {
      wasRunning = _isRunning && !_isPaused;
      totalChars = _totalChars;
      operations = _operations;
    }

    if (operations.Length == 0)
    {
      await RebuildFromOriginal().ConfigureAwait(false);
      lock (_animationLock)
      {
        totalChars = _totalChars;
        operations = _operations;
      }
    }

    var normalizedPosition = Math.Clamp(position, 0, 1);
    var targetChar = (int)(normalizedPosition * totalChars);

    lock (_animationLock)
    {
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
    }

    await BuildDOMToIndex(targetChar).ConfigureAwait(false);

    var atStart = normalizedPosition == 0;
    var atEnd = normalizedPosition >= 1.0;

    int currentCharCount;
    lock (_animationLock)
    {
      if (atStart || atEnd)
      {
        _isRunning = false;
        _isPaused = false;
      }
      currentCharCount = _currentCharCount;
      totalChars = _totalChars;
    }

    await OnSeek
      .InvokeAsync(
        new TypewriterSeekEventArgs(
          Position: normalizedPosition,
          TargetChar: currentCharCount,
          TotalChars: totalChars,
          Percent: totalChars > 0 ? (currentCharCount / (double)totalChars) * 100 : 0,
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
          currentCharCount,
          totalChars,
          totalChars > 0 ? (currentCharCount / (double)totalChars) * 100 : 0
        )
      )
      .ConfigureAwait(false);

    if (atEnd)
    {
      await OnComplete.InvokeAsync().ConfigureAwait(false);
    }
  }

  public Task SeekToPercent(double percent) => Seek(percent / 100);

  public Task SeekToChar(int charIndex)
  {
    int totalChars;
    lock (_animationLock)
    {
      totalChars = _totalChars;
    }
    return totalChars == 0 ? Task.CompletedTask : Seek(charIndex / (double)totalChars);
  }

  public TypewriterProgressInfo GetProgress()
  {
    lock (_animationLock)
    {
      return new(
        Current: _currentCharCount,
        Total: _totalChars,
        Percent: _totalChars > 0 ? (_currentCharCount / (double)_totalChars) * 100 : 0,
        Position: _totalChars > 0 ? _currentCharCount / (double)_totalChars : 0
      );
    }
  }
}

