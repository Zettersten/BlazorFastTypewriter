using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter.Demo.Components.Pages;

public partial class Home
{
  // Hero
  private Typewriter? _heroTypewriter;

  // Basic
  private Typewriter? _basicTypewriter;
  private bool _basicRunning;
  private bool _basicPaused;
  private TypewriterProgressInfo? _basicProgress;

  // Hero handlers
  private void HandleHeroComplete()
  {
    // Auto-restart hero after delay
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

  // Basic handlers
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
