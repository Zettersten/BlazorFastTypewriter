using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter.Demo.Components.Pages;

public partial class PlaybackControls
{
  // Controls
  private Typewriter? _controlTypewriter;
  private bool _controlRunning;
  private bool _controlPaused;
  private string _controlStatus = "Ready";
  private TypewriterProgressInfo? _controlProgress;

  // Progress
  private Typewriter? _progressTypewriter;
  private bool _progressRunning;
  private double _progressPercent;
  private string _progressText = "Ready";

  // Dynamic
  private Typewriter? _dynamicTypewriter;

  // Control handlers
  private void HandleControlStart()
  {
    _controlRunning = true;
    _controlPaused = false;
    _controlStatus = "Running";
    StateHasChanged();
  }

  private void HandleControlPause()
  {
    _controlPaused = true;
    _controlStatus = "Paused";
    StateHasChanged();
  }

  private void HandleControlResume()
  {
    _controlPaused = false;
    _controlStatus = "Running";
    StateHasChanged();
  }

  private void HandleControlComplete()
  {
    _controlRunning = false;
    _controlPaused = false;
    _controlStatus = "Complete";
    StateHasChanged();
  }

  private async Task StartControl()
  {
    if (_controlTypewriter is not null)
    {
      await _controlTypewriter.Start();
    }
  }

  private async Task PauseControl()
  {
    if (_controlTypewriter is not null)
    {
      await _controlTypewriter.Pause();
    }
  }

  private async Task ResumeControl()
  {
    if (_controlTypewriter is not null)
    {
      await _controlTypewriter.Resume();
    }
  }

  private async Task CompleteControl()
  {
    if (_controlTypewriter is not null)
    {
      await _controlTypewriter.Complete();
    }
  }

  private async Task ResetControl()
  {
    if (_controlTypewriter is not null)
    {
      await _controlTypewriter.Reset();
      _controlStatus = "Ready";
    }
  }

  private void HandleControlProgress(TypewriterProgressEventArgs args)
  {
    _controlProgress = new TypewriterProgressInfo(args.Current, args.Total, args.Percent, args.Current / (double)args.Total);
    StateHasChanged();
  }

  private async Task HandlePlayPause()
  {
    if (_controlPaused)
    {
      await ResumeControl();
    }
    else if (!_controlRunning)
    {
      await StartControl();
    }
    else
    {
      await PauseControl();
    }
  }

  // Progress handlers
  private void HandleProgress(TypewriterProgressEventArgs args)
  {
    _progressPercent = args.Percent;
    _progressText = $"Progress: {args.Percent:F1}% ({args.Current}/{args.Total} characters)";
    StateHasChanged();
  }

  private void HandleProgressComplete()
  {
    _progressRunning = false;
    _progressText = "Complete!";
    StateHasChanged();
  }

  private async Task StartProgress()
  {
    if (_progressTypewriter is not null)
    {
      _progressRunning = true;
      _progressText = "Starting...";
      await _progressTypewriter.Start();
    }
  }

  private async Task ResetProgress()
  {
    if (_progressTypewriter is not null)
    {
      await _progressTypewriter.Reset();
      _progressRunning = false;
      _progressPercent = 0;
      _progressText = "Ready";
    }
  }

  // Dynamic handlers
  private async Task SetText1()
  {
    if (_dynamicTypewriter is not null)
    {
      await _dynamicTypewriter.SetText(
        "<p>This is the <strong>first</strong> dynamic text update!</p>"
      );
      await _dynamicTypewriter.Start();
    }
  }

  private async Task SetText2()
  {
    if (_dynamicTypewriter is not null)
    {
      await _dynamicTypewriter.SetText(
        "<p>This is the <em>second</em> dynamic text update with different content.</p>"
      );
      await _dynamicTypewriter.Start();
    }
  }

  private async Task SetTextHtml()
  {
    if (_dynamicTypewriter is not null)
    {
      await _dynamicTypewriter.SetText(
        "<div><h3>HTML Content</h3><p>You can set <strong>rich HTML</strong> content dynamically!</p><ul><li>List item 1</li><li>List item 2</li></ul></div>"
      );
      await _dynamicTypewriter.Start();
    }
  }

  private async Task ResetDynamic()
  {
    if (_dynamicTypewriter is not null)
    {
      await _dynamicTypewriter.SetText(
        "<p>Initial content. Click the buttons below to change this text dynamically.</p>"
      );
      await _dynamicTypewriter.Reset();
    }
  }
}
