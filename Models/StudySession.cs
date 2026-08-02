namespace SmartStudy.Models;

public sealed class StudySession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime EndedAt { get; set; } = DateTime.Now;
    public int DurationMinutes { get; set; }
    public bool WasCompleted { get; set; } = true;
}
