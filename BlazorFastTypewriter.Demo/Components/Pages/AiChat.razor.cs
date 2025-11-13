using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorFastTypewriter.Demo.Components.Pages;

public partial class AiChat
{
  private readonly List<ChatMessage> _chatMessages = [];
  private string _chatInput = string.Empty;
  private int _aiChatSpeed = 150;
  private bool _isAiTyping;

  private void HandleAiSpeedChange(ChangeEventArgs e)
  {
    if (int.TryParse(e.Value?.ToString(), out var newSpeed))
    {
      _aiChatSpeed = newSpeed;
    }
  }

  private void HandleAiMessageComplete(ChatMessage message)
  {
    _isAiTyping = false;
    StateHasChanged();
  }

  private async Task HandleChatKeyDown(KeyboardEventArgs e)
  {
    if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(_chatInput) && !_isAiTyping)
    {
      await SendChatMessage();
    }
  }

  private async Task SendChatMessage()
  {
    if (string.IsNullOrWhiteSpace(_chatInput) || _isAiTyping)
      return;

    // Add user message
    var userMessage = new ChatMessage
    {
      Content = _chatInput,
      IsUser = true,
      Time = DateTime.Now
    };
    _chatMessages.Add(userMessage);

    var userInput = _chatInput;
    _chatInput = string.Empty;
    _isAiTyping = true;
    StateHasChanged();

    // Simulate AI processing delay
    await Task.Delay(300);

    // Generate AI response based on input
    var aiResponse = GenerateAiResponse(userInput);
    var aiMessage = new ChatMessage
    {
      IsUser = false,
      Time = DateTime.Now,
      TypewriterContent = builder => builder.AddMarkupContent(0, aiResponse),
      ShouldAutoStart = true // Enable autostart for this message
    };
    _chatMessages.Add(aiMessage);
    StateHasChanged();
  }

  private async Task SetPrompt(string prompt)
  {
    _chatInput = prompt;
    await SendChatMessage();
  }

  private string GenerateAiResponse(string input)
  {
    var lowerInput = input.ToLowerInvariant();

    return lowerInput switch
    {
      var s when s.Contains("hello") || s.Contains("hi")
        => "<p>Hello! 👋 I'm an <strong>AI assistant</strong> powered by the BlazorFastTypewriter component. How can I help you today?</p>",

      var s when s.Contains("blazor")
        => "<p>Great question! <strong>Blazor</strong> is a framework for building interactive web applications using <em>C#</em> instead of JavaScript. This typewriter component is built specifically for Blazor and demonstrates <code>real-time text streaming</code> capabilities.</p>",

      var s when s.Contains("typewriter") || s.Contains("component")
        => "<p>The <strong>BlazorFastTypewriter</strong> component is perfect for creating engaging user experiences! Here are some key features:</p><ul><li>⚡ High-performance character-by-character animation</li><li>🎨 Full HTML and formatting support</li><li>🎮 Complete programmatic control (play, pause, resume)</li><li>♿ Accessibility with ARIA live regions</li></ul>",

      var s when s.Contains("speed") || s.Contains("fast") || s.Contains("slow")
        => "<p>You can control the typing speed using the <code>Speed</code> parameter! Try adjusting the slider above to see different speeds. The speed is measured in <strong>characters per second</strong>, giving you precise control over the animation timing.</p>",

      var s when s.Contains("how") && s.Contains("work")
        => "<p>The component works by:</p><ol><li>Extracting the DOM structure from your content</li><li>Breaking it down into character operations</li><li>Animating each character with configurable delays</li><li>Preserving all HTML tags and formatting</li></ol><p>It's optimized for <em>minimal allocations</em> using modern .NET 10 features!</p>",

      var s when s.Contains("chat") || s.Contains("ai")
        => "<p>This chat demo showcases how you can use the typewriter component for <strong>AI chat applications</strong>! It's perfect for:</p><ul><li>💬 Chatbots and virtual assistants</li><li>🤖 AI response streaming</li><li>📚 Interactive tutorials</li><li>🎮 Game dialogues</li></ul>",

      var s when s.Contains("thank")
        => "<p>You're very welcome! 😊 Feel free to explore the other demos on this page to see more capabilities of the <strong>BlazorFastTypewriter</strong> component.</p>",

      var s when s.Contains("help")
        => "<p>I'd be happy to help! You can ask me about:</p><ul><li>How the Blazor typewriter component works</li><li>Features and capabilities</li><li>Usage examples and best practices</li><li>Performance and optimization</li></ul><p>Just type your question below! 💡</p>",

      _
        => $"<p>That's interesting! You mentioned <em>\"{input}\"</em>. The <strong>BlazorFastTypewriter</strong> component can animate any HTML content with character-by-character precision. Try asking me about Blazor, the typewriter component, or how to use it!</p>"
    };
  }

  private sealed class ChatMessage
  {
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Time { get; set; }
    public RenderFragment? TypewriterContent { get; set; }
    public bool ShouldAutoStart { get; set; }
  }
}
