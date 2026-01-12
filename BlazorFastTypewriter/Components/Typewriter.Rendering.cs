namespace BlazorFastTypewriter;

public partial class Typewriter
{
  private void ShowCurrentHtml()
  {
    CurrentContent = _currentHtmlFragment;
    StateHasChanged();
  }

  private void ShowOriginalContent()
  {
    CurrentContent = _originalContent;
    StateHasChanged();
  }
}

