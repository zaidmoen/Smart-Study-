using System.Text.Json.Serialization;
using SmartStudy.Core;

namespace SmartStudy.Models;

public sealed class StudyTask : ObservableObject
{
    private string _title = string.Empty;
    private string _subject = string.Empty;
    private string _notes = string.Empty;
    private DateTime _dueDate = DateTime.Today.AddDays(1);
    private int _estimatedMinutes = 45;
    private int _actualFocusMinutes;
    private StudyPriority _priority = StudyPriority.Medium;
    private bool _isCompleted;
    private DateTime? _completedAt;

    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value?.Trim() ?? string.Empty);
    }

    public string Subject
    {
        get => _subject;
        set
        {
            if (SetProperty(ref _subject, value?.Trim() ?? string.Empty))
            {
                OnPropertyChanged(nameof(SubjectInitial));
                OnPropertyChanged(nameof(AccentHex));
            }
        }
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value?.Trim() ?? string.Empty);
    }

    public DateTime DueDate
    {
        get => _dueDate;
        set
        {
            if (SetProperty(ref _dueDate, value))
            {
                NotifyDateProperties();
            }
        }
    }

    public int EstimatedMinutes
    {
        get => _estimatedMinutes;
        set
        {
            var normalized = Math.Clamp(value, 5, 600);
            if (SetProperty(ref _estimatedMinutes, normalized))
            {
                OnPropertyChanged(nameof(DurationText));
            }
        }
    }

    public int ActualFocusMinutes
    {
        get => _actualFocusMinutes;
        set => SetProperty(ref _actualFocusMinutes, Math.Max(0, value));
    }

    public StudyPriority Priority
    {
        get => _priority;
        set
        {
            if (SetProperty(ref _priority, value))
            {
                OnPropertyChanged(nameof(PriorityLabel));
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (SetProperty(ref _isCompleted, value))
            {
                CompletedAt = value ? DateTime.Now : null;
                NotifyDateProperties();
            }
        }
    }

    public DateTime? CompletedAt
    {
        get => _completedAt;
        set => SetProperty(ref _completedAt, value);
    }

    [JsonIgnore]
    public string SubjectInitial => string.IsNullOrWhiteSpace(Subject) ? "؟" : Subject.Trim()[0].ToString();

    [JsonIgnore]
    public string AccentHex
    {
        get
        {
            string[] palette = ["#6C63FF", "#0EA5E9", "#14B8A6", "#F97316", "#EC4899", "#8B5CF6", "#22C55E"];
            var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(Subject ?? string.Empty) & 0x7FFFFFFF;
            return palette[hash % palette.Length];
        }
    }

    [JsonIgnore]
    public string PriorityLabel => Priority switch
    {
        StudyPriority.Low => "منخفضة",
        StudyPriority.Medium => "متوسطة",
        StudyPriority.High => "عالية",
        StudyPriority.Critical => "حرجة",
        _ => "متوسطة"
    };

    [JsonIgnore]
    public string DurationText => EstimatedMinutes >= 60
        ? $"{EstimatedMinutes / 60}س {EstimatedMinutes % 60:00}د"
        : $"{EstimatedMinutes} دقيقة";

    [JsonIgnore]
    public bool IsOverdue => !IsCompleted && DueDate.Date < DateTime.Today;

    [JsonIgnore]
    public bool IsDueToday => !IsCompleted && DueDate.Date == DateTime.Today;

    [JsonIgnore]
    public bool IsUpcoming => !IsCompleted && DueDate.Date > DateTime.Today;

    [JsonIgnore]
    public string DueText
    {
        get
        {
            if (IsCompleted)
            {
                return CompletedAt is null ? "مكتملة" : $"اكتملت {CompletedAt:dd/MM}";
            }

            var days = (DueDate.Date - DateTime.Today).Days;
            return days switch
            {
                < 0 => $"متأخرة {Math.Abs(days)} يوم",
                0 => "موعدها اليوم",
                1 => "موعدها غدًا",
                <= 7 => $"متبقي {days} أيام",
                _ => DueDate.ToString("dd MMM yyyy")
            };
        }
    }

    private void NotifyDateProperties()
    {
        OnPropertyChanged(nameof(IsOverdue));
        OnPropertyChanged(nameof(IsDueToday));
        OnPropertyChanged(nameof(IsUpcoming));
        OnPropertyChanged(nameof(DueText));
    }
}
