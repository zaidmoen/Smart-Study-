using System;
using System.Globalization;
using System.Windows.Media;

namespace WpfApp1.Models
{
    public class SubjectOverview
    {
        public string Name { get; set; }
        public double HoursStudied { get; set; }
        public double HoursTarget { get; set; }
        public int CompletionPercent { get; set; }
        public string StatusText { get; set; }
        public Brush AccentBrush { get; set; }
        public Brush SoftAccentBrush { get; set; }

        public string CompletionLabel => CompletionPercent + "%";

        public string HoursSummary => string.Format(CultureInfo.InvariantCulture, "{0:0.0} من {1:0.0} ساعة", HoursStudied, HoursTarget);

        public double ProgressWidth => 250.0 * CompletionPercent / 100.0;
    }
}
