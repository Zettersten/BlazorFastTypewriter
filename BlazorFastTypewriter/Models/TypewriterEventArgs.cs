namespace BlazorFastTypewriter;

public readonly record struct TypewriterProgressEventArgs(int Current, int Total, double Percent);

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

public readonly record struct TypewriterProgressInfo(
  int Current,
  int Total,
  double Percent,
  double Position
);
