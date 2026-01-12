using System;
using System.Threading.Tasks;
using BlazorFastTypewriter;
using BlazorFastTypewriter.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorFastTypewriter.Tests;

public sealed class TypewriterTests : IDisposable
{
  private readonly BunitContext _ctx = new();
  private readonly BunitJSInterop _jsInterop;
  private readonly BunitJSModuleInterop _module;

  public TypewriterTests()
  {
    _jsInterop = _ctx.JSInterop;
    _module = _jsInterop.SetupModule("http://localhost/_content/BlazorFastTypewriter/Components/Typewriter.razor.js");

    _module.Setup<bool>("waitForElement").SetResult(true);
    _module.Setup<bool>("checkReducedMotion").SetResult(false);
  }

  public void Dispose() => _ctx.Dispose();

  [Fact]
  public void Renders_dir_attribute_when_configured()
  {
    var cut = _ctx.Render<Typewriter>(ps => ps
      .Add(p => p.Autostart, false)
      .Add(p => p.Dir, "rtl")
      .AddChildContent("<p>Hello</p>"));

    Assert.Contains("dir=\"rtl\"", cut.Markup);
  }

  [Fact]
  public async Task Start_extracts_dom_and_completes()
  {
    _module.Setup<DomStructure>("extractStructure")
      .SetResult(new DomStructure([
        new DomNode(type: "text", text: "Hi")
      ]));

    var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    var cut = _ctx.Render<Typewriter>(ps => ps
      .Add(p => p.Autostart, false)
      .Add(p => p.MinDuration, 0)
      .Add(p => p.MaxDuration, 1000)
      .Add(p => p.Speed, 1000)
      .Add(p => p.OnComplete, EventCallback.Factory.Create(this, () => completed.TrySetResult()))
      .AddChildContent("<p>Hi</p>"));

    await cut.Instance.Start();

    cut.WaitForAssertion(
      () => Assert.True(completed.Task.IsCompleted),
      timeout: TimeSpan.FromSeconds(2)
    );
  }

  [Fact]
  public async Task SeekToChar_renders_expected_partial_content()
  {
    _module.Setup<DomStructure>("extractStructure")
      .SetResult(new DomStructure([
        new DomNode(type: "text", text: "Hello")
      ]));

    var cut = _ctx.Render<Typewriter>(ps => ps
      .Add(p => p.Autostart, false)
      .Add(p => p.MinDuration, 0)
      .Add(p => p.MaxDuration, 1000)
      .Add(p => p.Speed, 1000)
      .AddChildContent("<p>Hello</p>"));

    await cut.Instance.Start();
    await cut.Instance.SeekToChar(1);

    cut.WaitForAssertion(
      () => Assert.Contains("H", cut.Markup),
      timeout: TimeSpan.FromSeconds(2)
    );
  }
}
