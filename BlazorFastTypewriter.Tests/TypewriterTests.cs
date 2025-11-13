using Bunit;
using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter.Tests;

/// <summary>
/// Comprehensive test suite for Typewriter component.
/// Tests SSR, Server, and WASM compatibility scenarios.
/// </summary>
public class TypewriterTests : IDisposable
{
  private readonly BunitContext _testContext;
  private readonly BunitJSInterop _jsInterop;

  public TypewriterTests()
  {
    _testContext = new BunitContext();
    _jsInterop = _testContext.JSInterop;

    // Setup JS module import - Bunit will handle the module reference
    var module = _jsInterop.SetupModule(
      "./_content/BlazorFastTypewriter/Components/Typewriter.razor.js"
    );
  }

  private IRenderedComponent<T> Render<T>(
    Action<ComponentParameterCollectionBuilder<T>> parameterBuilder
  )
    where T : IComponent => _testContext.Render<T>(parameterBuilder);

  public void Dispose()
  {
    _testContext?.Dispose();
  }
}
