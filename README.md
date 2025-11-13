# Blazor Fast Typewriter

[![NuGet](https://img.shields.io/nuget/v/BlazorFastTypewriter.svg)](https://www.nuget.org/packages/BlazorFastTypewriter/)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/Zettersten/BlazorFastTypewriter)

A high-performance typewriter component for Blazor that animates text character-by-character with full HTML support. Built with .NET 10 features for optimal performance, AOT compilation, and aggressive trimming.

> **Why another typewriter?** Shipping to WebAssembly or native AOT targets demands components that are deterministic, trimming-safe, and optimized from the first render. This component was built from the ground up with those goals, using modern .NET 10 features for maximum performance.

![Sample 1](https://github.com/Zettersten/BlazorFastTypewriter/blob/main/sample-1.gif?raw=true)
![Sample 2](https://github.com/Zettersten/BlazorFastTypewriter/blob/main/sample-2.gif?raw=true)
![Sample 3](https://github.com/Zettersten/BlazorFastTypewriter/blob/main/sample-3.gif?raw=true)

## Live Demo

🚀 **[View the interactive demo](https://zettersten.github.io/BlazorFastTypewriter/)**

## Table of Contents

- [Quick Start](#quick-start)
- [Features](#features)
- [Basic Usage](#basic-usage)
- [API Reference](#api-reference)
  - [Parameters](#parameters)
  - [Methods](#methods)
  - [Properties](#properties)
  - [Events](#events)
- [Advanced Usage](#advanced-usage)
  - [Seek & Scrubbing](#seek--scrubbing)
  - [Dynamic Content Updates](#dynamic-content-updates)
  - [Accessibility](#accessibility)
  - [Production Builds](#production-builds)
- [Technical Details](#technical-details)
- [Testing](#testing)
- [License](#license)

## Quick Start

### 1. Install the package

```bash
dotnet add package BlazorFastTypewriter
```

### 2. Add the namespace to `_Imports.razor`

```razor
@using BlazorFastTypewriter
```

### 3. Use the component

```razor
<Typewriter Speed="100">
    <p>Welcome to Blazor Fast Typewriter!</p>
</Typewriter>
```

That's it! CSS and JavaScript are automatically included via Blazor's static web assets.

## Features

### 🎯 Core Features
- **Full HTML Support** — Preserves tags, links, code blocks, and nested structures
- **High Performance** — Optimized with modern .NET 10 features (`ImmutableArray.Builder`, collection expressions, pattern matching)
- **Bidirectional Text** — Supports LTR and RTL text direction
- **Composable** — Works with any child content: text, components, images, or complex layouts

### ⚙️ Build Features
- **Trimming-Friendly** — Library is marked as trimmable and ships without reflection
- **AOT Ready** — Validated against Native AOT constraints with `RunAOTCompilation` enabled
- **Zero Configuration** — CSS and JavaScript are automatically bundled via static web assets

### 🎮 Control Features
- **Playback Control** — Start, pause, resume, complete, and reset with event callbacks
- **Seek & Scrubbing** — Jump to any position in the animation with `Seek()`, `SeekToPercent()`, or `SeekToChar()`
- **Progress Tracking** — Real-time progress events every 10 characters
- **Dynamic Content** — Update content programmatically with `SetText()` methods

### ♿ Accessibility Features
- **ARIA Live Regions** — Automatic `aria-live="polite"` and `aria-atomic="false"` for screen readers
- **Reduced Motion Support** — Respects `prefers-reduced-motion` media query when enabled
- **Semantic Markup** — Proper ARIA labels and role attributes

## Basic Usage

### Simple Animation

```razor
<Typewriter Speed="100" Autostart="true">
    <p>A simple typewriter with <strong>HTML support</strong>.</p>
</Typewriter>
```

### Manual Playback Control

```razor
<Typewriter @ref="_typewriter" Speed="60" Autostart="false">
    <p>Click the buttons to control the animation.</p>
</Typewriter>

<button @onclick="() => _typewriter?.Start()">Start</button>
<button @onclick="() => _typewriter?.Pause()">Pause</button>
<button @onclick="() => _typewriter?.Resume()">Resume</button>
<button @onclick="() => _typewriter?.Complete()">Complete</button>

@code {
    private Typewriter? _typewriter;
}
```

### Progress Tracking

```razor
<Typewriter Speed="80" OnProgress="HandleProgress">
    <p>Content to animate...</p>
</Typewriter>

<p>Progress: @_progress%</p>

@code {
    private double _progress = 0;
    
    private void HandleProgress(TypewriterProgressEventArgs args)
    {
        _progress = args.Percent;
    }
}
```

### RTL (Right-to-Left) Support

```razor
<Typewriter Dir="rtl" Speed="80">
    <p>يدعم المكوّن <strong>النصوص العربية</strong> مع الحفاظ على الاتجاه الصحيح.</p>
</Typewriter>
```

### Rich HTML Content

```razor
<Typewriter Speed="60">
    <div>
        <h2>Rich Content</h2>
        <p>Supports <strong>bold</strong>, <em>italic</em>, and <a href="#">links</a>.</p>
        <ul>
            <li>Lists with <code>inline code</code></li>
            <li>Nested <strong>formatting</strong></li>
        </ul>
    </div>
</Typewriter>
```

## API Reference

### Parameters

| Parameter | Type | Default | Description |
|:----------|:-----|:--------|:------------|
| `ChildContent` | `RenderFragment?` | `null` | Content to animate. Supports any HTML markup. |
| `Speed` | `int` | `100` | Typing speed in characters per second. |
| `MinDuration` | `int` | `100` | Minimum animation duration in milliseconds. |
| `MaxDuration` | `int` | `30000` | Maximum animation duration in milliseconds. |
| `Autostart` | `bool` | `true` | Auto-start animation on load. Set to `false` for manual control. |
| `Dir` | `string` | `"ltr"` | Text direction: `"ltr"` or `"rtl"`. |
| `RespectMotionPreference` | `bool` | `false` | Respect `prefers-reduced-motion` media query. |
| `AriaLabel` | `string?` | `null` | ARIA label for the container region. |
| `OnStart` | `EventCallback` | — | Fired when animation starts. |
| `OnPause` | `EventCallback` | — | Fired when animation pauses. |
| `OnResume` | `EventCallback` | — | Fired when animation resumes. |
| `OnComplete` | `EventCallback` | — | Fired when animation completes. |
| `OnReset` | `EventCallback` | — | Fired when component resets. |
| `OnProgress` | `EventCallback<TypewriterProgressEventArgs>` | — | Fired every 10 characters with progress info. |
| `OnSeek` | `EventCallback<TypewriterSeekEventArgs>` | — | Fired when seeking to a new position. |

### Methods

| Method | Description |
|:-------|:------------|
| `Task Start()` | Start the animation from the beginning. |
| `Task Pause()` | Pause the current animation. |
| `Task Resume()` | Resume a paused animation. |
| `Task Complete()` | Complete the animation instantly. |
| `Task Reset()` | Reset the component, clearing content and state. |
| `Task SetText(RenderFragment content)` | Replace content with a new `RenderFragment` and reset. |
| `Task SetText(string html)` | Replace content with an HTML string and reset. |
| `Task Seek(double position)` | Seek to a position (0.0 to 1.0). Pauses if animating. |
| `Task SeekToPercent(double percent)` | Seek to a percentage (0 to 100). |
| `Task SeekToChar(int charIndex)` | Seek to a specific character index. |
| `TypewriterProgressInfo GetProgress()` | Get current progress information. |

### Properties

| Property | Type | Description |
|:---------|:-----|:------------|
| `IsRunning` | `bool` | Whether the component is currently animating. |
| `IsPaused` | `bool` | Whether the component is currently paused. |

### Events

#### TypewriterProgressEventArgs

Provides progress information:
- `Current` (`int`) — Characters animated so far
- `Total` (`int`) — Total characters to animate
- `Percent` (`double`) — Percentage complete (0-100)

#### TypewriterSeekEventArgs

Provides seek information:
- `Position` (`double`) — Normalized position (0.0 to 1.0)
- `TargetChar` (`int`) — Character index seeked to
- `TotalChars` (`int`) — Total number of characters
- `Percent` (`double`) — Percentage complete (0-100)
- `WasRunning` (`bool`) — Whether animation was running before seek
- `CanResume` (`bool`) — Whether animation can be resumed
- `AtStart` (`bool`) — Whether seek landed at start
- `AtEnd` (`bool`) — Whether seek landed at end

#### TypewriterProgressInfo

Returned by `GetProgress()`:
- `Current` (`int`) — Current character count
- `Total` (`int`) — Total character count
- `Percent` (`double`) — Percentage complete (0-100)
- `Position` (`double`) — Normalized position (0.0 to 1.0)

## Advanced Usage

### Seek & Scrubbing

Jump to any position in the animation with full scrubbing support:

```razor
<Typewriter @ref="_typewriter" Speed="60" OnProgress="UpdatePosition">
    <p>Content to animate with seek support...</p>
</Typewriter>

<label>
    Position: @_position%
    <input type="range" min="0" max="100" value="@_position" 
           @oninput="e => SeekToPosition(e)" />
</label>

<button @onclick="() => _typewriter?.Seek(0)">Start</button>
<button @onclick="() => _typewriter?.Seek(0.5)">50%</button>
<button @onclick="() => _typewriter?.Seek(1)">End</button>

@code {
    private Typewriter? _typewriter;
    private double _position = 0;
    
    private async Task SeekToPosition(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), out var value))
        {
            _position = value;
            await (_typewriter?.SeekToPercent(value) ?? Task.CompletedTask);
        }
    }
    
    private void UpdatePosition(TypewriterProgressEventArgs args)
    {
        _position = args.Percent;
    }
}
```

### Dynamic Content Updates

Update content programmatically at runtime:

```razor
<Typewriter @ref="_typewriter" Autostart="false">
    @_content
</Typewriter>

<button @onclick="UpdateContent">Update Content</button>

@code {
    private Typewriter? _typewriter;
    private RenderFragment _content = builder => 
        builder.AddMarkupContent(0, "<p>Initial content</p>");
    
    private async Task UpdateContent()
    {
        await (_typewriter?.SetText("<p>New <strong>dynamic</strong> content!</p>") 
            ?? Task.CompletedTask);
        await (_typewriter?.Start() ?? Task.CompletedTask);
    }
}
```

### Accessibility

**Reduced Motion Support**

Respects user preferences for reduced motion:

```razor
<Typewriter RespectMotionPreference="true" Speed="100">
    <p>This animation respects user motion preferences.</p>
</Typewriter>
```

**ARIA Labels**

Provide context for screen readers:

```razor
<Typewriter AriaLabel="Chat message being typed">
    <p>Message content...</p>
</Typewriter>
```

**Best Practices**
- Keep `Speed` between 50-150 chars/sec for comfortable reading
- Enable `RespectMotionPreference` for accessibility compliance
- Provide `AriaLabel` when typewriter conveys essential information
- Use semantic HTML inside `ChildContent`
- Consider offering pause controls for longer animations

### Production Builds

The component is optimized for trimming and Native AOT compilation:

```bash
dotnet publish -c Release \
  -p:PublishTrimmed=true \
  -p:TrimMode=link \
  -p:RunAOTCompilation=true
```

**Notes:**
- The library opts into invariant globalization to minimize ICU payload size
- If your app requires full globalization, override `InvariantGlobalization` in your project file
- No reflection is used — fully trimming-safe
- Validated with trimming analyzers and Native AOT constraints

## Technical Details

### Architecture

**DOM Extraction** — Uses JavaScript interop to extract rendered DOM structure, preserving all HTML tags and attributes.

**Operation Queue** — Converts DOM structure into an immutable array of operations (open tag, character, close tag) for efficient processing.

**Animation Loop** — Runs on background thread using `Task.Run` with proper cancellation token support for responsive UI.

**Thread Safety** — All UI updates use `InvokeAsync` to ensure thread-safe rendering and prevent race conditions.

### Modern .NET 10 Features

- **Collection Expressions** — `[]` for empty arrays, `[..]` for spread operations
- **ImmutableArray.Builder** — Efficient immutable array building without allocations
- **Pattern Matching** — Modern syntax like `is null or { Length: 0 }`
- **Primary Constructors** — Records use concise primary constructor syntax
- **Lock** — Uses new `Lock` type for thread-safe operations
- **Random.Shared** — Thread-safe randomization for character delays

### Performance Optimizations

- **Minimal Allocations** — Pre-allocated `StringBuilder` with capacity and `ImmutableArray.Builder`
- **Cancellation Tokens** — Proper disposal to prevent memory leaks
- **Efficient Parsing** — Single-pass DOM structure parsing
- **Smart Delays** — Only delays for character operations, not tag operations
- **Generation Counter** — Efficient animation lifecycle management

## Testing

The project includes comprehensive BUnit tests covering:
- Component rendering and lifecycle
- Playback control methods
- Event callbacks and parameter forwarding
- Edge cases and error handling

Run tests locally:

```bash
dotnet test
```

## License

MIT
