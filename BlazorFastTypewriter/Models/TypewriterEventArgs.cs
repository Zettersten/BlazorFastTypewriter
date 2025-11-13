namespace BlazorFastTypewriter;

/// <summary>
/// Event arguments for progress events.
/// </summary>
public sealed record TypewriterProgressEventArgs(int Current, int Total, double Percent);

/// <summary>
/// Event arguments for seek events.
/// </summary>
public sealed record TypewriterSeekEventArgs(
  double Position,
  int TargetChar,
  int TotalChars,
  double Percent,
  bool WasRunning,
  bool CanResume,
  bool AtStart,
  bool AtEnd
);

/// <summary>
/// Progress information returned by GetProgress().
/// </summary>
public sealed record TypewriterProgressInfo(
  int Current,
  int Total,
  double Percent,
  double Position
);
