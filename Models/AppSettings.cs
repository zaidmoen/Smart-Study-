namespace SmartStudy.Models;

public sealed class AppSettings
{
    public int DailyGoalMinutes { get; set; } = 120;
    public int FocusMinutes { get; set; } = 25;
    public int BreakMinutes { get; set; } = 5;
    public bool AutoStartBreak { get; set; }
}
