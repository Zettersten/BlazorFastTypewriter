using System.Collections.Immutable;
using System.Text;
using BlazorFastTypewriter.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter;

/// <summary>
/// Animation logic for the Typewriter component.
/// </summary>
public partial class Typewriter
{
  /// <summary>
  /// Rebuilds the operations array from the original content.
  /// </summary>
  private async Task RebuildFromOriginal()
  {
    if (_originalContent is null || _jsModule is null || !_isInitialized)
      return;

    try
    {
      // Ensure content is rendered
      CurrentContent = _originalContent;
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);
      await Task.Delay(100).ConfigureAwait(false);

      var structure = await _jsModule.InvokeAsync<DomStructure>("extractStructure", [_containerId])
        .ConfigureAwait(false);

      if (structure is not null)
      {
        _operations = _domParser.ParseDomStructure(structure);
        _totalChars = _operations.Count(static op => op.Type == OperationType.Char);
      }
    }
    catch (Exception)
    {
      _operations = [];
      _totalChars = 0;
    }
  }

  /// <summary>
  /// Builds and renders content up to a specific character index (for seeking).
  /// </summary>
  private async Task BuildDOMToIndex(int targetChar)
  {
    // Clear and reset
    _currentCharCount = 0;
    _currentIndex = 0;

    if (targetChar <= 0)
    {
      // Set to empty content instead of null to avoid showing ChildContent fallback
      CurrentContent = static builder => { };
      await InvokeAsync(StateHasChanged).ConfigureAwait(false);
      return;
    }

    // Build HTML up to target character - pre-allocate capacity
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

    // Update content
    var html = currentHtml.ToString();
    await InvokeAsync(() =>
    {
      CurrentContent = builder => builder.AddMarkupContent(0, html);
      StateHasChanged();
    }).ConfigureAwait(false);
  }

  /// <summary>
  /// Core animation loop that renders content character by character.
  /// </summary>
  private async Task AnimateAsync(
    int generation,
    int baseDelay,
    int totalChars,
    CancellationToken cancellationToken
  )
  {
    // Pre-allocate StringBuilder with estimated capacity
    var currentHtml = new StringBuilder(capacity: 1024);

    // Rebuild existing content up to current index (for resume support)
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
        // Use a longer delay when paused to reduce CPU usage
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
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
          _currentCharCount++;
          break;

        case OperationType.CloseTag:
          currentHtml.Append(op.TagHtml);
          break;
      }

      _currentIndex = i + 1;

      // Update content via InvokeAsync to ensure thread safety
      var html = currentHtml.ToString();
      await InvokeAsync(() =>
      {
        CurrentContent = builder => builder.AddMarkupContent(0, html);
        StateHasChanged();
      }).ConfigureAwait(false);

      if (op.Type == OperationType.Char && _currentCharCount % 10 == 0 && totalChars > 0)
      {
        await OnProgress.InvokeAsync(
          new TypewriterProgressEventArgs(
            _currentCharCount,
            totalChars,
            (_currentCharCount / (double)totalChars) * 100
          )
        ).ConfigureAwait(false);
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
    
    // Always fire final progress event at 100%
    await OnProgress.InvokeAsync(
      new TypewriterProgressEventArgs(totalChars, totalChars, 100.0)
    ).ConfigureAwait(false);
    
    await InvokeAsync(() =>
    {
      CurrentContent = _originalContent;
      StateHasChanged();
    }).ConfigureAwait(false);
    await OnComplete.InvokeAsync().ConfigureAwait(false);
  }
}
