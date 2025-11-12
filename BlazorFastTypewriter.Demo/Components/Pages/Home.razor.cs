using BlazorFastTypewriter;
using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter.Demo.Components.Pages;

public partial class Home
{
  // Hero
  private Typewriter? _heroTypewriter;

  // Basic
  private Typewriter? _basicTypewriter;
  private bool _basicRunning;

  // Speed
  private Typewriter? _speedTypewriter;
  private bool _speedRunning;
  private int _speed = 100;

  // HTML
  private Typewriter? _htmlTypewriter;
  private bool _htmlRunning;

  // Controls
  private Typewriter? _controlTypewriter;
  private bool _controlRunning;
  private bool _controlPaused;
  private string _controlStatus = "Ready";

  // Progress
  private Typewriter? _progressTypewriter;
  private bool _progressRunning;
  private double _progressPercent;
  private string _progressText = "Ready";

  // RTL
  private Typewriter? _rtlTypewriter;
  private bool _rtlRunning;

  // Dynamic
  private Typewriter? _dynamicTypewriter;

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
  private async Task StartBasic()
  {
    if (_basicTypewriter is not null)
    {
      _basicRunning = true;
      await _basicTypewriter.Start();
    }
  }

  private async Task ResetBasic()
  {
    if (_basicTypewriter is not null)
    {
      await _basicTypewriter.Reset();
      _basicRunning = false;
    }
  }

  // Speed handlers
  private void HandleSpeedChange(ChangeEventArgs e)
  {
    if (int.TryParse(e.Value?.ToString(), out var newSpeed))
    {
      _speed = newSpeed;
    }
  }

  private async Task StartSpeed()
  {
    if (_speedTypewriter is not null)
    {
      _speedRunning = true;
      await _speedTypewriter.Start();
    }
  }

  private async Task ResetSpeed()
  {
    if (_speedTypewriter is not null)
    {
      await _speedTypewriter.Reset();
      _speedRunning = false;
    }
  }

  // HTML handlers
  private async Task StartHtml()
  {
    if (_htmlTypewriter is not null)
    {
      _htmlRunning = true;
      await _htmlTypewriter.Start();
    }
  }

  private async Task ResetHtml()
  {
    if (_htmlTypewriter is not null)
    {
      await _htmlTypewriter.Reset();
      _htmlRunning = false;
    }
  }

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

  private void PauseControl()
  {
    _controlTypewriter?.Pause();
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

  // RTL handlers
  private async Task StartRtl()
  {
    if (_rtlTypewriter is not null)
    {
      _rtlRunning = true;
      await _rtlTypewriter.Start();
    }
  }

  private async Task ResetRtl()
  {
    if (_rtlTypewriter is not null)
    {
      await _rtlTypewriter.Reset();
      _rtlRunning = false;
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
