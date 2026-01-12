using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter.Demo.Components.Pages;

public partial class Basics
{
  private Typewriter? _basicTypewriter;
  private bool _basicRunning;
  private bool _basicPaused;
  private TypewriterProgressInfo? _basicProgress;

  private Typewriter? _speedTypewriter;
  private bool _speedRunning;
  private bool _speedPaused;
  private int _speed = 100;
  private TypewriterProgressInfo? _speedProgress;

  private Typewriter? _htmlTypewriter;
  private bool _htmlRunning;
  private bool _htmlPaused;
  private TypewriterProgressInfo? _htmlProgress;

  private Typewriter? _rtlTypewriter;
  private bool _rtlRunning;
  private bool _rtlPaused;
  private TypewriterProgressInfo? _rtlProgress;

  private Typewriter? _batchTypewriter;
  private bool _batchRunning;
  private int _renderBatchSize = 5;
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

  private void HandleSpeedComplete()
  {
    _speedRunning = false;
    _speedPaused = false;
    StateHasChanged();
  }

  private async Task StartSpeed()
  {
    if (_speedTypewriter is not null)
    {
      _speedRunning = true;
      _speedPaused = false;
      await _speedTypewriter.Start();
    }
  }

  private async Task PauseSpeed()
  {
    if (_speedTypewriter is not null)
    {
      await _speedTypewriter.Pause();
      _speedPaused = true;
      StateHasChanged();
    }
  }

  private async Task ResumeSpeed()
  {
    if (_speedTypewriter is not null)
    {
      await _speedTypewriter.Resume();
      _speedPaused = false;
      StateHasChanged();
    }
  }

  private async Task ResetSpeed()
  {
    if (_speedTypewriter is not null)
    {
      await _speedTypewriter.Reset();
      _speedRunning = false;
      _speedPaused = false;
    }
  }

  private void HandleHtmlComplete()
  {
    _htmlRunning = false;
    _htmlPaused = false;
    StateHasChanged();
  }

  private async Task StartHtml()
  {
    if (_htmlTypewriter is not null)
    {
      _htmlRunning = true;
      _htmlPaused = false;
      await _htmlTypewriter.Start();
    }
  }

  private async Task PauseHtml()
  {
    if (_htmlTypewriter is not null)
    {
      await _htmlTypewriter.Pause();
      _htmlPaused = true;
      StateHasChanged();
    }
  }

  private async Task ResumeHtml()
  {
    if (_htmlTypewriter is not null)
    {
      await _htmlTypewriter.Resume();
      _htmlPaused = false;
      StateHasChanged();
    }
  }

  private async Task ResetHtml()
  {
    if (_htmlTypewriter is not null)
    {
      await _htmlTypewriter.Reset();
      _htmlRunning = false;
      _htmlPaused = false;
    }
  }

  private void HandleRtlComplete()
  {
    _rtlRunning = false;
    _rtlPaused = false;
    StateHasChanged();
  }

  private async Task StartRtl()
  {
    if (_rtlTypewriter is not null)
    {
      _rtlRunning = true;
      _rtlPaused = false;
      await _rtlTypewriter.Start();
    }
  }

  private async Task PauseRtl()
  {
    if (_rtlTypewriter is not null)
    {
      await _rtlTypewriter.Pause();
      _rtlPaused = true;
      StateHasChanged();
    }
  }

  private async Task ResumeRtl()
  {
    if (_rtlTypewriter is not null)
    {
      await _rtlTypewriter.Resume();
      _rtlPaused = false;
      StateHasChanged();
    }
  }

  private async Task ResetRtl()
  {
    if (_rtlTypewriter is not null)
    {
      await _rtlTypewriter.Reset();
      _rtlRunning = false;
      _rtlPaused = false;
    }
  }

  private void HandleBasicProgress(TypewriterProgressEventArgs args)
  {
    _basicProgress = new TypewriterProgressInfo(
      args.Current,
      args.Total,
      args.Percent,
      args.Current / (double)args.Total
    );
    StateHasChanged();
  }

  private void HandleSpeedProgress(TypewriterProgressEventArgs args)
  {
    _speedProgress = new TypewriterProgressInfo(
      args.Current,
      args.Total,
      args.Percent,
      args.Current / (double)args.Total
    );
    StateHasChanged();
  }

  private void HandleHtmlProgress(TypewriterProgressEventArgs args)
  {
    _htmlProgress = new TypewriterProgressInfo(
      args.Current,
      args.Total,
      args.Percent,
      args.Current / (double)args.Total
    );
    StateHasChanged();
  }

  private void HandleRtlProgress(TypewriterProgressEventArgs args)
  {
    _rtlProgress = new TypewriterProgressInfo(
      args.Current,
      args.Total,
      args.Percent,
      args.Current / (double)args.Total
    );
    StateHasChanged();
  }

  private async Task HandleSpeedChangeFromControls(int newSpeed)
  {
    _speed = newSpeed;
    StateHasChanged();
  }

  private async Task StartBatch()
  {
    if (_batchTypewriter is null)
      return;

    _batchRunning = true;
    await _batchTypewriter.Start();
  }

  private async Task ResetBatch()
  {
    if (_batchTypewriter is null)
      return;

    _batchRunning = false;
    await _batchTypewriter.Reset();
  }

  private void HandleBatchComplete()
  {
    _batchRunning = false;
    StateHasChanged();
  }

  private void HandleRenderBatchSizeInput(ChangeEventArgs e)
  {
    if (int.TryParse(e.Value?.ToString(), out var value))
    {
      _renderBatchSize = Math.Clamp(value, 1, 20);
    }
  }
}
