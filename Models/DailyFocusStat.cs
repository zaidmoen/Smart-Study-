namespace SmartStudy.Models;

public sealed class DailyFocusStat
{
    public string DayLabel { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public int Minutes { get; init; }
    public double BarHeight { get; init; }
    public string MinutesLabel => Minutes == 0 ? "—" : $"{Minutes}د";
}
