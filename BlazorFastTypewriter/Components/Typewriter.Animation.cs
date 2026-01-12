using System.Text;

namespace BlazorFastTypewriter;

public partial class Typewriter
{
  private async Task RebuildFromOriginal()
  {
    if (_originalContent is null || _jsModule is null || !_isInitialized)
      return;

    try
    {
      lock (_animationLock)
      {
        _isExtracting = true;
      }
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);

      var elementReady = await _jsModule
        .InvokeAsync<bool>("waitForElement", [$"{_containerId}-extract", 3000])
        .ConfigureAwait(false);

      if (!elementReady)
      {
        lock (_animationLock)
        {
          _operations = [];
          _totalChars = 0;
          _isExtracting = false;
        }
        return;
      }

      var structure = await _jsModule
        .InvokeAsync<DomStructure>("extractStructure", [$"{_containerId}-extract"])
        .ConfigureAwait(false);

      if (structure is not null)
      {
        var operations = DomParsingService.ParseDomStructure(structure);
        var totalChars = CountChars(operations);
        lock (_animationLock)
        {
          _operations = operations;
          _totalChars = totalChars;
        }
      }

      lock (_animationLock)
      {
        _isExtracting = false;
      }
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    }
    catch (Exception)
    {
      lock (_animationLock)
      {
        _operations = [];
        _totalChars = 0;
        _isExtracting = false;
      }
    }
  }

  private async Task BuildDOMToIndex(int targetChar)
  {
    ImmutableArray<NodeOperation> operations;
    lock (_animationLock)
    {
      operations = _operations;
      _currentCharCount = 0;
      _currentIndex = 0;
    }

    if (targetChar <= 0)
    {
      _currentHtml = string.Empty;
      CurrentContent = EmptyFragment;
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);
      return;
    }

    var currentHtml = new StringBuilder(capacity: 1024);
    var charCount = 0;
    int finalIndex = 0;

    for (var i = 0; i < operations.Length; i++)
    {
      var op = operations[i];

      switch (op.Type)
      {
        case OperationType.OpenTag:
          currentHtml.Append(op.TagHtml);
          break;

        case OperationType.Char:
          if (charCount >= targetChar)
          {
            finalIndex = i;
            goto BuildComplete;
          }
          currentHtml.Append(op.Char);
          charCount++;
          break;

        case OperationType.CloseTag:
          currentHtml.Append(op.TagHtml);
          break;
      }

      finalIndex = i + 1;
    }

    BuildComplete:
    lock (_animationLock)
    {
      _currentIndex = finalIndex;
      _currentCharCount = charCount;
    }

    _currentHtml = currentHtml.ToString();
    await InvokeAsync(ShowCurrentHtml).ConfigureAwait(false);
  }

  private async Task AnimateAsync(
    int generation,
    int baseDelay,
    int totalChars,
    CancellationToken cancellationToken
  )
  {
    var currentHtml = new StringBuilder(capacity: 1024);
    ImmutableArray<NodeOperation> operations;
    int startIndex;
    int currentCharCount;

    lock (_animationLock)
    {
      operations = _operations;
      startIndex = _currentIndex;
      currentCharCount = _currentCharCount;
    }

    for (var i = 0; i < startIndex; i++)
    {
      if (i >= operations.Length)
        break;

      var op = operations[i];
      switch (op.Type)
      {
        case OperationType.OpenTag:
          currentHtml.Append(op.TagHtml);
          break;
        case OperationType.Char:
          currentHtml.Append(op.Char);
          break;
        case OperationType.CloseTag:
          currentHtml.Append(op.TagHtml);
          break;
      }
    }

    var startCharCount = currentCharCount;
    var batchSize = EffectiveRenderBatchSize;

    for (var i = startIndex; i < operations.Length; i++)
    {
      bool shouldContinue;
      bool isPaused;
      lock (_animationLock)
      {
        shouldContinue = generation == _generation && _isRunning && !cancellationToken.IsCancellationRequested;
        isPaused = _isPaused;
      }

      if (!shouldContinue)
        return;

      if (isPaused)
      {
        lock (_animationLock)
        {
          _currentIndex = i;
        }
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        i--;
        continue;
      }

      var op = operations[i];

      switch (op.Type)
      {
        case OperationType.OpenTag:
          currentHtml.Append(op.TagHtml);
          break;

        case OperationType.Char:
          currentHtml.Append(op.Char);
          currentCharCount++;
          break;

        case OperationType.CloseTag:
          currentHtml.Append(op.TagHtml);
          break;
      }

      lock (_animationLock)
      {
        _currentIndex = i + 1;
        _currentCharCount = currentCharCount;
      }

      if (op.Type == OperationType.Char)
      {
        var typedThisRun = currentCharCount - startCharCount;
        var shouldRender = typedThisRun == 1 || (typedThisRun % batchSize == 0) || i == operations.Length - 1;
        if (shouldRender)
        {
          _currentHtml = currentHtml.ToString();
          await InvokeAsync(ShowCurrentHtml).ConfigureAwait(false);
        }
      }

      if (op.Type == OperationType.Char && currentCharCount % 10 == 0 && totalChars > 0)
      {
        await OnProgress
          .InvokeAsync(
            new TypewriterProgressEventArgs(
              currentCharCount,
              totalChars,
              (currentCharCount / (double)totalChars) * 100
            )
          )
          .ConfigureAwait(false);
      }

      if (op.Type == OperationType.Char)
      {
        var itemDelay = baseDelay + Random.Shared.Next(0, 6);
        if (itemDelay > 0)
        {
          await Task.Delay(itemDelay, cancellationToken).ConfigureAwait(false);
        }
      }
    }

    lock (_animationLock)
    {
      if (generation == _generation)
      {
        _isRunning = false;
        _currentCharCount = totalChars;
      }
    }

    await OnProgress
      .InvokeAsync(new TypewriterProgressEventArgs(totalChars, totalChars, 100.0))
      .ConfigureAwait(false);

    await InvokeAsync(ShowOriginalContent).ConfigureAwait(false);
    await OnComplete.InvokeAsync().ConfigureAwait(false);
  }
}
