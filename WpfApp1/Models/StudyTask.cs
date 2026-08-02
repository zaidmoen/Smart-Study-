using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Linq;

namespace WpfApp1.Models
{
    public class StudyTask : INotifyPropertyChanged
    {
        private bool _isDone;
        private string _title;
        private string _subject;
        private DateTime _dueDate;
        private string _estimateText;
        private string _studyMode;
        private int _confidencePercent;
        private int _priorityWeight;
        private string _priorityLabel;
        private Brush _accentBrush;
        private Brush _softAccentBrush;

        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Subject { get => _subject; set => SetProperty(ref _subject, value); }
        public DateTime DueDate { get => _dueDate; set => SetProperty(ref _dueDate, value); }
        public string EstimateText { get => _estimateText; set => SetProperty(ref _estimateText, value); }
        public string StudyMode { get => _studyMode; set => SetProperty(ref _studyMode, value); }
        public int ConfidencePercent { get => _confidencePercent; set => SetProperty(ref _confidencePercent, value); }
        public int PriorityWeight { get => _priorityWeight; set => SetProperty(ref _priorityWeight, value); }
        public string PriorityLabel { get => _priorityLabel; set => SetProperty(ref _priorityLabel, value); }
        public Brush AccentBrush { get => _accentBrush; set => SetProperty(ref _accentBrush, value); }
        public Brush SoftAccentBrush { get => _softAccentBrush; set => SetProperty(ref _softAccentBrush, value); }

        public bool IsDone
        {
            get { return _isDone; }
            set
            {
                if (SetProperty(ref _isDone, value))
                {
                    OnPropertyChanged(nameof(StatusLabel));
                    OnPropertyChanged(nameof(CardOpacity));
                }
            }
        }

        public string SubjectInitial => string.IsNullOrWhiteSpace(Subject) ? "د" : Subject.Trim().Substring(0, 1);

        public string DueText
        {
            get
            {
                int days = (DueDate.Date - DateTime.Today).Days;
                if (days <= 0) return "موعدها اليوم";
                if (days == 1) return "موعدها غدًا";
                return "بعد " + days + " أيام";
            }
        }

        public string ConfidenceText => "ثقة " + ConfidencePercent + "%";

        public string TaskLaneText
        {
            get
            {
                int minutes = ParseEstimateMinutes();
                if (PriorityWeight >= 3 || minutes >= 45) return "Deep Focus";
                if (minutes <= 20) return "Quick Win";
                return "Steady Pace";
            }
        }

        public string StatusLabel => IsDone ? "مكتمل" : PriorityLabel;

        public double CardOpacity => IsDone ? 0.6 : 1.0;

        private int ParseEstimateMinutes()
        {
            string digits = new string((EstimateText ?? string.Empty).Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int minutes) ? minutes : 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
