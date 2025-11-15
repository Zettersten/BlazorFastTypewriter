namespace BlazorFastTypewriter.Demo.Components.Pages;

public partial class Home
{
  private Typewriter? _heroTypewriter;
  private Typewriter? _basicTypewriter;
  private bool _basicRunning;
  private bool _basicPaused;
  private TypewriterProgressInfo? _basicProgress;

  private void HandleHeroComplete()
  {
    _ = Task.Run(async () =>
    {
      await Task.Delay(3000);
      if (_heroTypewriter is not null)
      {
        await _heroTypewriter.Reset();
        await _heroTypewriter.Start();
      }
    });
  }

  private void HandleBasicComplete()
  {
    _basicRunning = false;
    _basicPaused = false;
    StateHasChanged();
  }

  private async Task StartBasic()
  {
    if (_basicTypewriter is not null)
    {
      _basicRunning = true;
      _basicPaused = false;
      await _basicTypewriter.Start();
    }
  }

  private async Task PauseBasic()
  {
    if (_basicTypewriter is not null)
    {
      await _basicTypewriter.Pause();
      _basicPaused = true;
      StateHasChanged();
    }
  }

  private async Task ResumeBasic()
  {
    if (_basicTypewriter is not null)
    {
      await _basicTypewriter.Resume();
      _basicPaused = false;
      StateHasChanged();
    }
  }

  private async Task ResetBasic()
  {
    if (_basicTypewriter is not null)
    {
      await _basicTypewriter.Reset();
      _basicRunning = false;
      _basicPaused = false;
    }
  }
}
