using System;
using System.Globalization;
using System.Windows.Media;

namespace WpfApp1.Models
{
    public class StudySession
    {
        public string Subject { get; set; }
        public string Topic { get; set; }
        public DateTime StartAt { get; set; }
        public int DurationMinutes { get; set; }
        public Brush AccentBrush { get; set; }
        public Brush SoftAccentBrush { get; set; }

        public string SubjectInitial => string.IsNullOrWhiteSpace(Subject) ? "د" : Subject.Trim().Substring(0, 1);

        public string TimeRange => StartAt.ToString("HH:mm", CultureInfo.InvariantCulture) + " - " +
                                  StartAt.AddMinutes(DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture);

        public string SessionLabel
        {
            get
            {
                if (DurationMinutes >= 50) return "Deep Focus";
                if (DurationMinutes >= 35) return "Sharp Sprint";
                return "Quick Review";
            }
        }
    }
}
