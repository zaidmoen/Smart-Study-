namespace SmartStudy.Models;

public sealed class SubjectStat
{
    public string Subject { get; init; } = string.Empty;
    public int Minutes { get; init; }
    public int CompletedTasks { get; init; }
    public double Percentage { get; init; }
    public string AccentHex { get; init; } = "#6C63FF";
    public string MinutesLabel => Minutes >= 60 ? $"{Minutes / 60}س {Minutes % 60}د" : $"{Minutes} دقيقة";
}
