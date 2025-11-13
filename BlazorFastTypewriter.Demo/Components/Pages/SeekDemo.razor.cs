using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter.Demo.Components.Pages;

public partial class SeekDemo
{
  private Typewriter? _seekTypewriter;
  private bool _seekRunning;
  private bool _seekPaused;
  private double _seekPercent;
  private TypewriterProgressInfo? _seekInfo;

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
    _seekInfo = new TypewriterProgressInfo(
      args.TargetChar,
      args.TotalChars,
      args.Percent,
      args.Position
    );

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
    _seekInfo = new TypewriterProgressInfo(
      args.Current,
      args.Total,
      args.Percent,
      args.Current / (double)args.Total
    );
    StateHasChanged();
  }

  private void HandleSeekComplete()
  {
    _seekRunning = false;
    _seekPaused = false;
    _seekPercent = 100;
    StateHasChanged();
  }

  private async Task HandlePlayPause()
  {
    if (_seekPaused)
    {
      await ResumeSeek();
    }
    else if (!_seekRunning)
    {
      await StartSeek();
    }
    else
    {
      await PauseSeek();
    }
  }

  private async Task HandleSeekFromControls(double percent)
  {
    if (_seekTypewriter is not null)
    {
      await _seekTypewriter.SeekToPercent(percent);
    }
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
      _seekInfo = null;
    }
  }
}
