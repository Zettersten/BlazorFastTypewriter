namespace BlazorFastTypewriter;

public partial class Typewriter
{
  private int ComputeDurationMs(int totalChars)
  {
    if (totalChars <= 0)
      return 0;

    var minDuration = Math.Max(0, MinDuration);
    var maxDuration = Math.Max(minDuration, MaxDuration);
    var durationFromSpeed = (int)Math.Round((totalChars / (double)EffectiveSpeed) * 1000);
    return Math.Clamp(durationFromSpeed, minDuration, maxDuration);
  }

  private int ComputeDelayMs(int totalChars)
  {
    if (totalChars <= 0)
      return 0;

    var duration = ComputeDurationMs(totalChars);
    return Math.Max(8, duration / totalChars);
  }
}

