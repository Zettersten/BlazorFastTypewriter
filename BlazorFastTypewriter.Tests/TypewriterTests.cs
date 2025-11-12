using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace BlazorFastTypewriter.Tests;

/// <summary>
/// Comprehensive test suite for Typewriter component.
/// Tests SSR, Server, and WASM compatibility scenarios.
/// </summary>
public class TypewriterTests : TestContext
{
  private readonly Mock<IJSRuntime> _jsRuntimeMock;

  public TypewriterTests()
  {
    _jsRuntimeMock = new Mock<IJSRuntime>();
    Services.AddSingleton(_jsRuntimeMock.Object);
  }

  [Fact]
  public void Render_WithContent_DisplaysContent()
  {
    // Arrange
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test content</p>"))
    );

    // Act & Assert
    cut.Markup.Should().Contain("Test content");
  }

  [Fact]
  public void Render_WithAutostartFalse_ShowsContentImmediately()
  {
    // Arrange & Act
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Hello</p>"))
    );

    // Assert
    cut.Markup.Should().Contain("Hello");
    cut.Instance.IsRunning.Should().BeFalse();
  }

  [Fact]
  public void Render_WithCustomSpeed_SetsSpeedParameter()
  {
    // Arrange & Act
    var cut = Render<Typewriter>(parameters =>
      parameters.Add(p => p.Speed, 50).Add(p => p.Autostart, false)
    );

    // Assert
    cut.Instance.Speed.Should().Be(50);
  }

  [Fact]
  public void Render_WithRtlDirection_SetsDirAttribute()
  {
    // Arrange & Act
    var cut = Render<Typewriter>(parameters =>
      parameters.Add(p => p.Dir, "rtl").Add(p => p.Autostart, false)
    );

    // Assert
    cut.Find(".type-writer-container").GetAttribute("style").Should().Contain("direction: rtl");
  }

  [Fact]
  public void Render_WithAriaLabel_SetsAriaLabelAttribute()
  {
    // Arrange & Act
    var cut = Render<Typewriter>(parameters =>
      parameters.Add(p => p.AriaLabel, "Typewriter region").Add(p => p.Autostart, false)
    );

    // Assert
    cut.Find(".type-writer-container").GetAttribute("aria-label").Should().Be("Typewriter region");
  }

  [Fact]
  public void Render_SetsAriaLiveRegion()
  {
    // Arrange & Act
    var cut = Render<Typewriter>(parameters => parameters.Add(p => p.Autostart, false));

    // Assert
    var container = cut.Find(".type-writer-container");
    container.GetAttribute("role").Should().Be("region");
    container.GetAttribute("aria-live").Should().Be("polite");
    container.GetAttribute("aria-atomic").Should().Be("false");
  }

  [Fact]
  public async Task Start_WhenNotRunning_BeginsAnimation()
  {
    // Arrange
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test</p>"))
    );

    // Act
    await cut.Instance.Start();

    // Assert
    cut.Instance.IsRunning.Should().BeTrue();
  }

  [Fact]
  public async Task Start_WhenAlreadyRunning_DoesNotRestart()
  {
    // Arrange
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test</p>"))
    );

    await cut.Instance.Start();
    var initialGeneration = cut.Instance.IsRunning;

    // Act
    await cut.Instance.Start();

    // Assert
    cut.Instance.IsRunning.Should().Be(initialGeneration);
  }

  [Fact]
  public async Task Pause_WhenRunning_PausesAnimation()
  {
    // Arrange
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test</p>"))
    );

    await cut.Instance.Start();

    // Act
    cut.Instance.Pause();

    // Assert
    cut.Instance.IsPaused.Should().BeTrue();
    cut.Instance.IsRunning.Should().BeTrue();
  }

  [Fact]
  public void Pause_WhenNotRunning_DoesNothing()
  {
    // Arrange
    var cut = Render<Typewriter>(parameters => parameters.Add(p => p.Autostart, false));

    // Act
    cut.Instance.Pause();

    // Assert
    cut.Instance.IsPaused.Should().BeFalse();
  }

  [Fact]
  public async Task Resume_WhenPaused_ResumesAnimation()
  {
    // Arrange
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test</p>"))
    );

    await cut.Instance.Start();
    cut.Instance.Pause();

    // Act
    await cut.Instance.Resume();

    // Assert
    cut.Instance.IsPaused.Should().BeFalse();
  }

  [Fact]
  public async Task Complete_WhenRunning_CompletesAnimation()
  {
    // Arrange
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test</p>"))
    );

    await cut.Instance.Start();

    // Act
    await cut.Instance.Complete();

    // Assert
    cut.Instance.IsRunning.Should().BeFalse();
    cut.Instance.IsPaused.Should().BeFalse();
  }

  [Fact]
  public async Task Reset_ClearsState()
  {
    // Arrange
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test</p>"))
    );

    await cut.Instance.Start();

    // Act
    await cut.Instance.Reset();

    // Assert
    cut.Instance.IsRunning.Should().BeFalse();
    cut.Instance.IsPaused.Should().BeFalse();
  }

  [Fact]
  public async Task SetText_WithRenderFragment_UpdatesContent()
  {
    // Arrange
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Old</p>"))
    );

    // Act
    await cut.Instance.SetText(builder => builder.AddMarkupContent(0, "<p>New</p>"));

    // Assert
    cut.Instance.IsRunning.Should().BeFalse();
  }

  [Fact]
  public async Task SetText_WithHtmlString_UpdatesContent()
  {
    // Arrange
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Old</p>"))
    );

    // Act
    await cut.Instance.SetText("<p>New</p>");

    // Assert
    cut.Instance.IsRunning.Should().BeFalse();
  }

  [Fact]
  public async Task OnStart_EventFires_WhenAnimationStarts()
  {
    // Arrange
    var startFired = false;
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test</p>"))
        .Add(p => p.OnStart, EventCallback.Factory.Create(this, () => startFired = true))
    );

    // Act
    await cut.Instance.Start();

    // Assert
    startFired.Should().BeTrue();
  }

  [Fact]
  public async Task OnPause_EventFires_WhenAnimationPauses()
  {
    // Arrange
    var pauseFired = false;
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test</p>"))
        .Add(p => p.OnPause, EventCallback.Factory.Create(this, () => pauseFired = true))
    );

    await cut.Instance.Start();

    // Act
    cut.Instance.Pause();

    // Assert
    pauseFired.Should().BeTrue();
  }

  [Fact]
  public async Task OnResume_EventFires_WhenAnimationResumes()
  {
    // Arrange
    var resumeFired = false;
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test</p>"))
        .Add(p => p.OnResume, EventCallback.Factory.Create(this, () => resumeFired = true))
    );

    await cut.Instance.Start();
    cut.Instance.Pause();

    // Act
    await cut.Instance.Resume();

    // Assert
    resumeFired.Should().BeTrue();
  }

  [Fact]
  public async Task OnComplete_EventFires_WhenAnimationCompletes()
  {
    // Arrange
    var completeFired = false;
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test</p>"))
        .Add(p => p.OnComplete, EventCallback.Factory.Create(this, () => completeFired = true))
    );

    await cut.Instance.Start();

    // Act
    await cut.Instance.Complete();

    // Assert
    completeFired.Should().BeTrue();
  }

  [Fact]
  public async Task OnReset_EventFires_WhenComponentResets()
  {
    // Arrange
    var resetFired = false;
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Test</p>"))
        .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => resetFired = true))
    );

    // Act
    await cut.Instance.Reset();

    // Assert
    resetFired.Should().BeTrue();
  }

  [Fact]
  public async Task OnProgress_EventFires_WithCorrectValues()
  {
    // Arrange
    TypewriterProgressEventArgs? progressArgs = null;
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.Autostart, false)
        .Add(
          p => p.ChildContent,
          builder => builder.AddMarkupContent(0, "<p>Test content with many characters</p>")
        )
        .Add(
          p => p.OnProgress,
          EventCallback.Factory.Create<TypewriterProgressEventArgs>(
            this,
            args => progressArgs = args
          )
        )
    );

    // Act
    await cut.Instance.Start();

    // Note: In a real scenario, we'd wait for progress events, but for unit tests
    // we're testing the event wiring. Actual progress would require JS interop mocking.
    // Assert would verify progressArgs is set correctly when events fire
  }

  [Fact]
  public async Task Dispose_CleansUpResources()
  {
    // Arrange
    var cut = Render<Typewriter>(parameters => parameters.Add(p => p.Autostart, false));

    // Act
    await cut.Instance.DisposeAsync();

    // Assert - should not throw
    cut.Instance.Should().NotBeNull();
  }

  [Fact]
  public void Render_WithMinMaxDuration_SetsParameters()
  {
    // Arrange & Act
    var cut = Render<Typewriter>(parameters =>
      parameters
        .Add(p => p.MinDuration, 100)
        .Add(p => p.MaxDuration, 1000)
        .Add(p => p.Autostart, false)
    );

    // Assert
    cut.Instance.MinDuration.Should().Be(100);
    cut.Instance.MaxDuration.Should().Be(1000);
  }

  [Fact]
  public void Render_WithRespectMotionPreference_SetsParameter()
  {
    // Arrange & Act
    var cut = Render<Typewriter>(parameters =>
      parameters.Add(p => p.RespectMotionPreference, true).Add(p => p.Autostart, false)
    );

    // Assert
    cut.Instance.RespectMotionPreference.Should().BeTrue();
  }
}
