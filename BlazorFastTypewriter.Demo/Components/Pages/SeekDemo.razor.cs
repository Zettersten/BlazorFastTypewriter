namespace BlazorFastTypewriter.Demo.Components.Pages;

public partial class SeekDemo
{
  private Typewriter? _seekTypewriter;
  private bool _seekRunning;
  private bool _seekPaused;
  private TypewriterProgressInfo? _seekInfo;

  private async Task SeekToPosition(double position)
  {
    if (_seekTypewriter is not null)
    {
      await _seekTypewriter.Seek(position);
    }
  }

  private void HandleSeek(TypewriterSeekEventArgs args)
  {
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
    StateHasChanged();
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
      _seekInfo = null;
    }
  }
}
