namespace BlazorFastTypewriter;

/// <summary>
/// Event arguments for progress events.
/// Optimized as readonly record struct for zero allocations.
/// </summary>
public readonly record struct TypewriterProgressEventArgs(int Current, int Total, double Percent);

/// <summary>
/// Event arguments for seek events.
/// Optimized as readonly record struct for zero allocations.
/// </summary>
public readonly record struct TypewriterSeekEventArgs(
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
/// Optimized as readonly record struct for zero allocations.
/// </summary>
public readonly record struct TypewriterProgressInfo(
  int Current,
  int Total,
  double Percent,
  double Position
);
