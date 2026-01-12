namespace BlazorFastTypewriter;

public partial class Typewriter
{
  private static int CountChars(ImmutableArray<NodeOperation> operations)
  {
    var count = 0;
    for (var i = 0; i < operations.Length; i++)
    {
      if (operations[i].Type == OperationType.Char)
        count++;
    }
    return count;
  }
}

