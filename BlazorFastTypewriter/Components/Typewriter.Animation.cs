using System.Collections.Immutable;
using System.Text;
using BlazorFastTypewriter.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter;

public partial class Typewriter
{
  private async Task RebuildFromOriginal()
  {
    if (_originalContent is null || _jsModule is null || !_isInitialized)
      return;

    try
    {
      _isExtracting = true;
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);
      await Task.Delay(150).ConfigureAwait(false);

      var elementReady = await _jsModule
        .InvokeAsync<bool>("waitForElement", [$"{_containerId}-extract", 3000])
        .ConfigureAwait(false);
      
      if (elementReady)
      {
        await Task.Delay(100).ConfigureAwait(false);
      }

      if (!elementReady)
      {
        _operations = [];
        _totalChars = 0;
        _isExtracting = false;
        return;
      }

      var structure = await _jsModule
        .InvokeAsync<DomStructure>("extractStructure", [$"{_containerId}-extract"])
        .ConfigureAwait(false);

      if (structure is not null)
      {
        _operations = DomParsingService.ParseDomStructure(structure);
        _totalChars = _operations.Count(static op => op.Type == OperationType.Char);
      }

      _isExtracting = false;
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    }
    catch (Exception)
    {
      _operations = [];
      _totalChars = 0;
      _isExtracting = false;
    }
  }

  private async Task BuildDOMToIndex(int targetChar)
  {
    _currentCharCount = 0;
    _currentIndex = 0;

    if (targetChar <= 0)
    {
      CurrentContent = static builder => { };
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);
      return;
    }

    var currentHtml = new StringBuilder(capacity: 1024);
    var charCount = 0;

    for (var i = 0; i < _operations.Length; i++)
    {
      var op = _operations[i];

      switch (op.Type)
      {
        case OperationType.OpenTag:
          currentHtml.Append(op.TagHtml);
          break;

        case OperationType.Char:
          if (charCount >= targetChar)
          {
            _currentIndex = i;
            goto BuildComplete;
          }
          currentHtml.Append(op.Char);
          charCount++;
          break;

        case OperationType.CloseTag:
          currentHtml.Append(op.TagHtml);
          break;
      }

      _currentIndex = i + 1;
    }

    BuildComplete:
    _currentCharCount = charCount;

    var html = currentHtml.ToString();
    await InvokeAsync(() =>
      {
        CurrentContent = builder => builder.AddMarkupContent(0, html);
        StateHasChanged();
      })
      .ConfigureAwait(false);
  }

  private async Task AnimateAsync(
    int generation,
    int baseDelay,
    int totalChars,
    CancellationToken cancellationToken
  )
  {
    var currentHtml = new StringBuilder(capacity: 1024);

    for (var i = 0; i < _currentIndex; i++)
    {
      var op = _operations[i];
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

    for (var i = _currentIndex; i < _operations.Length; i++)
    {
      if (generation != _generation || !_isRunning || cancellationToken.IsCancellationRequested)
        return;

      if (_isPaused)
      {
        _currentIndex = i;
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        i--;
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
          _currentCharCount++;
          break;

        case OperationType.CloseTag:
          currentHtml.Append(op.TagHtml);
          break;
      }

      _currentIndex = i + 1;

      var html = currentHtml.ToString();
      await InvokeAsync(() =>
        {
          CurrentContent = builder => builder.AddMarkupContent(0, html);
          StateHasChanged();
        })
        .ConfigureAwait(false);

      if (op.Type == OperationType.Char && _currentCharCount % 10 == 0 && totalChars > 0)
      {
        await OnProgress
          .InvokeAsync(
            new TypewriterProgressEventArgs(
              _currentCharCount,
              totalChars,
              (_currentCharCount / (double)totalChars) * 100
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

    _isRunning = false;
    _currentCharCount = totalChars;

    await OnProgress
      .InvokeAsync(new TypewriterProgressEventArgs(totalChars, totalChars, 100.0))
      .ConfigureAwait(false);

    await InvokeAsync(() =>
      {
        CurrentContent = _originalContent;
        StateHasChanged();
      })
      .ConfigureAwait(false);
    await OnComplete.InvokeAsync().ConfigureAwait(false);
  }
}
