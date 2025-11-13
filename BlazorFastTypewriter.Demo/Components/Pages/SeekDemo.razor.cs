using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter.Demo.Components.Pages;

public partial class SeekDemo
{
  private Typewriter? _seekTypewriter;
  private bool _seekRunning;
  private bool _seekPaused;
  private double _seekPercent;
  private string _seekInfo = "Ready";

  private async Task HandleSeekInput(ChangeEventArgs e)
  {
    if (double.TryParse(e.Value?.ToString(), out var value))
    {
      _seekPercent = value;
    }
  }

  private async Task HandleSeekChange(ChangeEventArgs e)
  {
    if (double.TryParse(e.Value?.ToString(), out var value) && _seekTypewriter is not null)
    {
      _seekPercent = value;
      await _seekTypewriter.SeekToPercent(value);
    }
  }

  private async Task SeekToPosition(double position)
  {
    if (_seekTypewriter is not null)
    {
      await _seekTypewriter.Seek(position);
    }
  }

  private void HandleSeek(TypewriterSeekEventArgs args)
  {
    _seekPercent = args.Percent;
    _seekInfo = $"Seeked to: {args.Percent:F1}% ({args.TargetChar}/{args.TotalChars} chars)";
    
    if (args.AtStart)
    {
      _seekRunning = false;
      _seekPaused = false;
    }
    else if (args.AtEnd)
    {
      _seekRunning = false;
      _seekPaused = false;
    }
    else if (args.WasRunning || args.CanResume)
    {
      _seekRunning = true;
      _seekPaused = true;
    }
    
    StateHasChanged();
  }

  private void HandleSeekProgress(TypewriterProgressEventArgs args)
  {
    _seekPercent = args.Percent;
    StateHasChanged();
  }

  private void HandleSeekComplete()
  {
    _seekRunning = false;
    _seekPaused = false;
    _seekPercent = 100;
    _seekInfo = "Complete!";
    StateHasChanged();
  }

  private async Task StartSeek()
  {
    if (_seekTypewriter is not null)
    {
      _seekRunning = true;
      _seekPaused = false;
      await _seekTypewriter.Start();
    }
  }

  private async Task PauseSeek()
  {
    if (_seekTypewriter is not null)
    {
      _seekPaused = true;
      await _seekTypewriter.Pause();
    }
  }

  private async Task ResumeSeek()
  {
    if (_seekTypewriter is not null)
    {
      _seekPaused = false;
      await _seekTypewriter.Resume();
    }
  }

  private async Task ResetSeek()
  {
    if (_seekTypewriter is not null)
    {
      await _seekTypewriter.Reset();
      _seekRunning = false;
      _seekPaused = false;
      _seekPercent = 0;
      _seekInfo = "Ready";
    }
  }
}
