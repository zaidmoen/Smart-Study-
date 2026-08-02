namespace SmartStudy.Models;

public sealed class AppState
{
    public List<StudyTask> Tasks { get; set; } = [];
    public List<StudySession> Sessions { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
    public DateTime LastSavedAt { get; set; } = DateTime.Now;
}
