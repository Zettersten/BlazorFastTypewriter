# Blazor Fast Typewriter

[![NuGet](https://img.shields.io/nuget/v/BlazorFastTypewriter.svg)](https://www.nuget.org/packages/BlazorFastTypewriter/)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/Zettersten/BlazorFastTypewriter)

Blazor Fast Typewriter is a high-performance typewriter component for Blazor that animates text character-by-character with full HTML support. Built from the ground up with .NET 10 features, it delivers smooth animations while embracing ahead-of-time (AOT) compilation and aggressive trimming so your applications remain lean without sacrificing fidelity.

> **Why another typewriter?** Because shipping to WebAssembly or native ahead-of-time targets demands components that are deterministic, trimming safe, and optimized from the first render. Blazor Fast Typewriter was built from the ground up with those goals in mind, using modern .NET 10 features like `ImmutableArray.Builder`, collection expressions, and pattern matching for maximum performance.

## Live Demo

🚀 **[View the interactive demo](https://zettersten.github.io/BlazorFastTypewriter/)**

## Highlights

- ⚡ **Full HTML Support.** Preserves tags including emphasis, links, code blocks, and nested structures while animating character-by-character.
- 🧭 **Bidirectional Text.** Supports LTR and RTL text direction via `Dir` parameter with proper rendering.
- 🪶 **Trimming-friendly by design.** The library is marked as trimmable, ships without reflection, and has analyzers enabled so you can confidently publish with `PublishTrimmed=true`.
- 🚀 **AOT ready.** `RunAOTCompilation` is enabled so the component is validated against Native AOT constraints during publish.
- 🧩 **Composable.** Works with any child content—text, components, images, or complex layouts.
- 🎯 **Accessibility First.** ARIA live regions, reduced motion support, and semantic markup.
- ⚙️ **Dynamic Content.** Update content programmatically with `SetText()` methods.
- 🎮 **Playback Control.** Methods for start, pause, resume, complete, and reset with event callbacks.

## Getting Started

### Installation

Install the package from NuGet:

```bash
dotnet add package BlazorFastTypewriter
```

### Setup

**1. Configure your Blazor app** in `Program.cs`:

For **Blazor WebAssembly**:
```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
// ... rest of your configuration
await builder.Build().RunAsync();
```

For **Blazor Server** or **Blazor Web App**:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents(); // If using WebAssembly interactivity
```

**2. CSS and JavaScript are automatically included** via Blazor's static web assets system. The component's styles (`Typewriter.razor.css`) and JavaScript module (`Typewriter.razor.js`) are bundled and served automatically—no manual script or link tags required.

**3. Import the namespace** in your `_Imports.razor`:
```razor
@using BlazorFastTypewriter
```

### Usage

**Basic example:**

```razor
@page "/typewriter-demo"
@using BlazorFastTypewriter

<Typewriter Speed="100" Autostart="true">
    <h2>Welcome to Blazor Fast Typewriter</h2>
    <p>A high-performance component for character-by-character text animation with <strong>HTML support</strong>.</p>
    <p>Perfect for <em>interactive storytelling</em>, <a href="#">educational tutorials</a>, and <code>dynamic content</code>.</p>
</Typewriter>
```

**With playback control:**

```razor
<Typewriter @ref="_typewriter" 
            Speed="60" 
            Autostart="false"
            OnStart="HandleStart"
            OnComplete="HandleComplete"
            OnProgress="HandleProgress">
    <p>Click the buttons below to control the animation.</p>
</Typewriter>

<button @onclick="Start">Start</button>
<button @onclick="Pause">Pause</button>
<button @onclick="Resume">Resume</button>
<button @onclick="Complete">Complete</button>
<button @onclick="Reset">Reset</button>

@code {
    private Typewriter? _typewriter;
    
    private async Task Start() => await _typewriter?.Start();
    private void Pause() => _typewriter?.Pause();
    private async Task Resume() => await _typewriter?.Resume();
    private async Task Complete() => await _typewriter?.Complete();
    private async Task Reset() => await _typewriter?.Reset();
    
    private void HandleStart() => Console.WriteLine("Animation started");
    private void HandleComplete() => Console.WriteLine("Animation completed");
    private void HandleProgress(TypewriterProgressEventArgs args) 
        => Console.WriteLine($"Progress: {args.Percent:F1}%");
}
```

**With RTL support:**

```razor
<Typewriter Dir="rtl" Speed="80">
    <p>يدعم المكوّن <strong>النصوص العربية</strong> مع الحفاظ على الاتجاه الصحيح.</p>
    <p>يمكن دمج <a href="#">الروابط</a> والنصوص الإنجليزية مثل <code>TypeWriter</code> داخل المحتوى العربي.</p>
</Typewriter>
```

**Dynamic content updates:**

```razor
<Typewriter @ref="_typewriter" Autostart="false">
    @_content
</Typewriter>

<button @onclick="UpdateContent">Update Content</button>

@code {
    private Typewriter? _typewriter;
    private RenderFragment _content = builder => builder.AddMarkupContent(0, "<p>Initial content</p>");
    
    private async Task UpdateContent()
    {
        await _typewriter?.SetText("<p>New content with <strong>HTML</strong> support!</p>");
        await _typewriter?.Start();
    }
}
```

**With reduced motion support:**

```razor
<Typewriter RespectMotionPreference="true" Speed="100">
    <p>This animation respects the user's motion preferences.</p>
    <p>If prefers-reduced-motion is enabled, content appears instantly.</p>
</Typewriter>
```

## Production Builds with Trimming & AOT

Blazor Fast Typewriter is validated with trimming analyzers and Native AOT so you can ship the smallest possible payloads. When publishing your application run:

```bash
dotnet publish -c Release -p:PublishTrimmed=true -p:TrimMode=link -p:RunAOTCompilation=true
```

The library opts into invariant globalization to minimize ICU payload size. If your app requires full globalization data, override `InvariantGlobalization` in your project file.

## Props

| Parameter                  | Type                                   | Default  | Description |
| :------------------------- | :------------------------------------- | :------- | :---------- |
| `ChildContent`             | `RenderFragment?`                      | `null`   | The content to be animated. Supports any HTML markup including nested structures, links, code blocks, and components. |
| `Speed`                    | `int`                                  | `100`    | Typing speed in characters per second. Higher values animate faster. |
| `MinDuration`              | `int`                                  | `50`     | Minimum animation duration in milliseconds. Ensures animations don't complete too quickly. |
| `MaxDuration`              | `int`                                  | `500`    | Maximum animation duration in milliseconds. Prevents animations from taking too long. |
| `Autostart`                | `bool`                                 | `true`   | Whether to automatically start the animation when the component loads. Set to `false` to control manually. |
| `Dir`                      | `string`                               | `"ltr"`  | Text direction: `"ltr"` for left-to-right or `"rtl"` for right-to-left. Affects rendering and animation flow. |
| `RespectMotionPreference`  | `bool`                                 | `false`  | Whether to respect the `prefers-reduced-motion` media query. When enabled, content appears instantly if the user prefers reduced motion. |
| `AriaLabel`                | `string?`                              | `null`   | ARIA label for the container region. Provides accessible description for screen readers. |
| `OnStart`                  | `EventCallback`                        | `default`| Invoked when the animation starts. |
| `OnPause`                  | `EventCallback`                        | `default`| Invoked when the animation is paused. |
| `OnResume`                 | `EventCallback`                        | `default`| Invoked when the animation resumes after being paused. |
| `OnComplete`               | `EventCallback`                        | `default`| Invoked when the animation completes and all content is displayed. |
| `OnReset`                  | `EventCallback`                        | `default`| Invoked when the component is reset and content is cleared. |
| `OnProgress`               | `EventCallback<TypewriterProgressEventArgs>` | `default` | Invoked every 10 characters during animation. Provides `Current`, `Total`, and `Percent` progress information. |

## Methods

| Method                     | Description |
| :------------------------- | :---------- |
| `Task Start()`             | Begins the animation from the start. If already running, does nothing. |
| `Task Pause()`             | Pauses the current animation. Animation can be resumed with `Resume()`. |
| `Task Resume()`            | Resumes a paused animation. Continues from where it was paused. |
| `Task Complete()`          | Completes the animation instantly, displaying all content immediately. |
| `Task Reset()`             | Resets the component, clearing content and state. Fires `OnReset` event. |
| `Task SetText(RenderFragment newContent)` | Replaces the content with a new `RenderFragment` and resets the component. |
| `Task SetText(string html)` | Replaces the content with an HTML string and resets the component. |

## Properties

| Property                   | Type      | Description |
| :------------------------- | :-------- | :---------- |
| `IsRunning`                | `bool`    | Gets whether the component is currently animating. |
| `IsPaused`                 | `bool`    | Gets whether the component is currently paused. |

## TypewriterProgressEventArgs

Progress events provide the following information:

- `Current` (`int`): Number of characters animated so far.
- `Total` (`int`): Total number of characters to animate.
- `Percent` (`double`): Percentage complete (0-100).

## Accessibility & Performance Tips

- **Semantic Markup**: Prefer semantic HTML inside `ChildContent` and provide `AriaLabel` when the typewriter conveys essential information.
- **Speed Control**: Keep `Speed` within a comfortable range (50-150 chars/sec) and consider offering controls to pause for accessibility compliance.
- **Reduced Motion**: Enable `RespectMotionPreference="true"` to automatically disable animations for users who request reduced motion.
- **ARIA Live Regions**: The component automatically sets `aria-live="polite"` and `aria-atomic="false"` for screen reader support.
- **Performance**: For large content, consider breaking it into smaller chunks or using `MaxDuration` to limit animation time.
- **Memory**: The component uses `ImmutableArray` and efficient DOM extraction to minimize allocations. Content is parsed once and reused.

## Technical Details

### Architecture

- **DOM Extraction**: Uses JavaScript interop to extract the rendered DOM structure, preserving HTML tags and attributes.
- **Operation Queue**: Converts DOM structure into an immutable array of operations (open tag, character, close tag).
- **Animation Loop**: Runs on a background thread using `Task.Run` with proper cancellation token support.
- **Thread Safety**: All UI updates use `InvokeAsync` to ensure thread-safe rendering.

### .NET 10 Features

- **Collection Expressions**: Uses `[]` for empty arrays and `[..]` for spread operations.
- **ImmutableArray.Builder**: Efficiently builds immutable arrays without intermediate allocations.
- **Pattern Matching**: Uses modern pattern matching with `is null or { Length: 0 }` syntax.
- **Primary Constructors**: Records use primary constructor syntax.
- **Random.Shared**: Uses thread-safe `Random.Shared` for character delay randomization.

### Performance Optimizations

- **Minimal Allocations**: Uses `StringBuilder` with pre-allocated capacity and `ImmutableArray.Builder` for efficient building.
- **Cancellation Tokens**: Proper disposal of `CancellationTokenSource` to prevent memory leaks.
- **Efficient Parsing**: Single-pass DOM structure parsing with recursive processing.
- **Smart Delays**: Only delays for character operations, not for tag operations.

## Testing

The solution includes automated BUnit tests covering rendering, lifecycle methods, event callbacks, and parameter forwarding. Run them locally with:

```bash
dotnet test
```

## License

MIT
