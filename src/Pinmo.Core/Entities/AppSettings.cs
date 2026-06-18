namespace Pinmo.Core.Entities;

public class AppSettings
{
    public int RequestTimeoutSeconds { get; set; } = 30;
    public CloseWindowAction CloseWindowAction { get; set; } = CloseWindowAction.Quit;
}
