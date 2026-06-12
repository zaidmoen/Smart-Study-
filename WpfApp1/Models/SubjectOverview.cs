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
        public int CompletedMinutes { get; set; }
        public int TotalMinutes { get; set; }
        public int RemainingMinutes { get; set; }
        public int CompletionPercent { get; set; }
        public int SharePercent { get; set; }
        public string StatusText { get; set; }
        public Brush AccentBrush { get; set; }
        public Brush SoftAccentBrush { get; set; }

        public string CompletionLabel => CompletionPercent + "%";
        public string ShareLabel => SharePercent + "% من الخطة";

        public string HoursSummary => string.Format(CultureInfo.InvariantCulture, "{0:0.0} من {1:0.0} ساعة", HoursStudied, HoursTarget);
        public string EffortSummary => FormatMinutes(CompletedMinutes) + " من " + FormatMinutes(TotalMinutes);
        public string RemainingSummary => RemainingMinutes <= 0 ? "مكتملة" : "متبقي " + FormatMinutes(RemainingMinutes);

        public double ProgressWidth => 250.0 * CompletionPercent / 100.0;
        public double ShareWidth => 250.0 * SharePercent / 100.0;

        private static string FormatMinutes(int minutes)
        {
            if (minutes < 60) return minutes + " دقيقة";

            int hours = minutes / 60;
            int rest = minutes % 60;
            return rest == 0 ? hours + " ساعة" : hours + "س " + rest + "د";
        }
    }
}
