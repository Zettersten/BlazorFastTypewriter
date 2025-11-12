using Microsoft.AspNetCore.Components;

namespace BlazorFastTypewriter;

public partial class Typewriter : ComponentBase, IAsyncDisposable
{
  public ValueTask DisposeAsync() => throw new NotImplementedException();
}
