using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter.Demo.Components.Pages;

public partial class Basics
{
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

  // RTL
  private Typewriter? _rtlTypewriter;
  private bool _rtlRunning;

  // Basic handlers
  private void HandleBasicComplete()
  {
    _basicRunning = false;
    StateHasChanged();
  }

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

  private void HandleSpeedComplete()
  {
    _speedRunning = false;
    StateHasChanged();
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
  private void HandleHtmlComplete()
  {
    _htmlRunning = false;
    StateHasChanged();
  }

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

  // RTL handlers
  private void HandleRtlComplete()
  {
    _rtlRunning = false;
    StateHasChanged();
  }

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
}
