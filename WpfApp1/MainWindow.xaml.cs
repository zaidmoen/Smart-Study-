using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfApp1
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly CultureInfo ArabicCulture = CreateArabicCulture();
        private readonly DispatcherTimer _clockTimer;

        private int _recommendationOffset;
        private string _currentDateText;
        private string _currentTimeText;
        private string _smartRecommendationTitle;
        private string _smartRecommendationBody;
        private string _completedTasksText;
        private string _pendingTasksText;
        private string _urgentTasksText;
        private string _plannedMinutesText;
        private string _taskListSubtitle;
        private string _dailyMissionText;
        private string _overallProgressText;
        private string _overallProgressSubtitle;
        private double _overallProgressWidth;
        private string _studyStreakText;
        private string _momentumText;
        private string _focusLaneText;
        private string _nextSessionText;
        private string _taskHeatText;
        private string _winningSubjectText;
        private string _recoverySubjectText;
        private string _newTaskTitle;
        private string _selectedSubjectOption;
        private string _selectedDurationOption;
        private string _selectedPriorityOption;
        private bool _isTaskListEmpty;

        public MainWindow()
        {
            Subjects = new ObservableCollection<SubjectOverview>();
            TodaySessions = new ObservableCollection<StudySession>();
            StudyTasks = new ObservableCollection<StudyTask>();
            SubjectOptions = new ObservableCollection<string>();
            DurationOptions = new ObservableCollection<string>();
            PriorityOptions = new ObservableCollection<string>();

            InitializeComponent();
            DataContext = this;

            SeedOptions();
            SeedData();
            ConfigureTaskSorting();
            ResetQuickAddFields();
            RefreshDashboard();

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromMinutes(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
        }

        public ObservableCollection<SubjectOverview> Subjects { get; private set; }

        public ObservableCollection<StudySession> TodaySessions { get; private set; }

        public ObservableCollection<StudyTask> StudyTasks { get; private set; }

        public ObservableCollection<string> SubjectOptions { get; private set; }

        public ObservableCollection<string> DurationOptions { get; private set; }

        public ObservableCollection<string> PriorityOptions { get; private set; }

        public string CurrentDateText
        {
            get { return _currentDateText; }
            set { SetProperty(ref _currentDateText, value); }
        }

        public string CurrentTimeText
        {
            get { return _currentTimeText; }
            set { SetProperty(ref _currentTimeText, value); }
        }

        public string SmartRecommendationTitle
        {
            get { return _smartRecommendationTitle; }
            set { SetProperty(ref _smartRecommendationTitle, value); }
        }

        public string SmartRecommendationBody
        {
            get { return _smartRecommendationBody; }
            set { SetProperty(ref _smartRecommendationBody, value); }
        }

        public string CompletedTasksText
        {
            get { return _completedTasksText; }
            set { SetProperty(ref _completedTasksText, value); }
        }

        public string PendingTasksText
        {
            get { return _pendingTasksText; }
            set { SetProperty(ref _pendingTasksText, value); }
        }

        public string UrgentTasksText
        {
            get { return _urgentTasksText; }
            set { SetProperty(ref _urgentTasksText, value); }
        }

        public string PlannedMinutesText
        {
            get { return _plannedMinutesText; }
            set { SetProperty(ref _plannedMinutesText, value); }
        }

        public string TaskListSubtitle
        {
            get { return _taskListSubtitle; }
            set { SetProperty(ref _taskListSubtitle, value); }
        }

        public string DailyMissionText
        {
            get { return _dailyMissionText; }
            set { SetProperty(ref _dailyMissionText, value); }
        }

        public string OverallProgressText
        {
            get { return _overallProgressText; }
            set { SetProperty(ref _overallProgressText, value); }
        }

        public string OverallProgressSubtitle
        {
            get { return _overallProgressSubtitle; }
            set { SetProperty(ref _overallProgressSubtitle, value); }
        }

        public double OverallProgressWidth
        {
            get { return _overallProgressWidth; }
            set { SetProperty(ref _overallProgressWidth, value); }
        }

        public string StudyStreakText
        {
            get { return _studyStreakText; }
            set { SetProperty(ref _studyStreakText, value); }
        }

        public string MomentumText
        {
            get { return _momentumText; }
            set { SetProperty(ref _momentumText, value); }
        }

        public string FocusLaneText
        {
            get { return _focusLaneText; }
            set { SetProperty(ref _focusLaneText, value); }
        }

        public string NextSessionText
        {
            get { return _nextSessionText; }
            set { SetProperty(ref _nextSessionText, value); }
        }

        public string TaskHeatText
        {
            get { return _taskHeatText; }
            set { SetProperty(ref _taskHeatText, value); }
        }

        public string WinningSubjectText
        {
            get { return _winningSubjectText; }
            set { SetProperty(ref _winningSubjectText, value); }
        }

        public string RecoverySubjectText
        {
            get { return _recoverySubjectText; }
            set { SetProperty(ref _recoverySubjectText, value); }
        }

        public string NewTaskTitle
        {
            get { return _newTaskTitle; }
            set { SetProperty(ref _newTaskTitle, value); }
        }

        public string SelectedSubjectOption
        {
            get { return _selectedSubjectOption; }
            set { SetProperty(ref _selectedSubjectOption, value); }
        }

        public string SelectedDurationOption
        {
            get { return _selectedDurationOption; }
            set { SetProperty(ref _selectedDurationOption, value); }
        }

        public string SelectedPriorityOption
        {
            get { return _selectedPriorityOption; }
            set { SetProperty(ref _selectedPriorityOption, value); }
        }

        public bool IsTaskListEmpty
        {
            get { return _isTaskListEmpty; }
            set { SetProperty(ref _isTaskListEmpty, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            RefreshDashboard();
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = (NewTaskTitle ?? string.Empty).Trim();
            string subject = (SelectedSubjectOption ?? string.Empty).Trim();
            string durationRaw = (SelectedDurationOption ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("اكتب اسم المهمة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = "عام";
            }

            // Standardize duration text
            string durationText = durationRaw;
            if (!durationText.Contains("دقيقة") && durationText.Any(char.IsDigit))
            {
                durationText += " دقيقة";
            }
            else if (string.IsNullOrWhiteSpace(durationText))
            {
                durationText = "30 دقيقة";
            }

            AddTask(new StudyTask
            {
                Title = title,
                Subject = subject,
                EstimateText = durationText,
                PriorityLabel = SelectedPriorityOption,
                DueDate = ResolveDueDate(SelectedPriorityOption),
                StudyMode = BuildStudyMode(subject),
                ConfidencePercent = 70,
                PriorityWeight = ResolvePriorityWeight(SelectedPriorityOption),
                AccentBrush = ResolveAccentBrush(subject),
                SoftAccentBrush = ResolveSoftAccentBrush(subject)
            });

            ResetQuickAddFields();
            RefreshDashboard();
        }

        private void GenerateSmartPlan_Click(object sender, RoutedEventArgs e)
        {
            _recommendationOffset++;
            RefreshDashboard();
        }

        private void StartFocus_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                SmartRecommendationTitle + Environment.NewLine + Environment.NewLine + SmartRecommendationBody,
                "غرفة التركيز",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void SeedOptions()
        {
            SubjectOptions.Clear();
            DurationOptions.Clear();

            PriorityOptions.Clear();
            PriorityOptions.Add("عادي");
            PriorityOptions.Add("مهم");
            PriorityOptions.Add("عاجل");
        }

        private void SeedData()
        {
            Subjects.Clear();
            TodaySessions.Clear();
            StudyTasks.Clear();
        }

        private void ConfigureTaskSorting()
        {
            ICollectionView taskView = CollectionViewSource.GetDefaultView(StudyTasks);
            taskView.SortDescriptions.Clear();
            taskView.SortDescriptions.Add(new SortDescription("IsDone", ListSortDirection.Ascending));
            taskView.SortDescriptions.Add(new SortDescription("DueDate", ListSortDirection.Ascending));
            taskView.SortDescriptions.Add(new SortDescription("PriorityWeight", ListSortDirection.Descending));
        }

        private void ResetQuickAddFields()
        {
            NewTaskTitle = string.Empty;
            SelectedSubjectOption = string.Empty;
            SelectedDurationOption = "30";
            SelectedPriorityOption = PriorityOptions.ElementAtOrDefault(1);
        }

        private void AddTask(StudyTask task)
        {
            task.PropertyChanged += Task_PropertyChanged;
            StudyTasks.Add(task);
        }

        private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsDone")
            {
                CollectionViewSource.GetDefaultView(StudyTasks).Refresh();
                RefreshDashboard();
            }
        }

        private void RefreshDashboard()
        {
            UpdateClock();
            UpdateTaskSummary();
            UpdateStudyInsights();
            UpdateRecommendation();
        }

        private void UpdateClock()
        {
            DateTime now = DateTime.Now;
            CurrentDateText = now.ToString("dddd، dd MMMM", ArabicCulture);
            CurrentTimeText = now.ToString("HH:mm", ArabicCulture);
        }

        private void UpdateTaskSummary()
        {
            int total = StudyTasks.Count;
            int completed = StudyTasks.Count(task => task.IsDone);
            int pending = total - completed;
            int urgent = StudyTasks.Count(task => !task.IsDone && (task.DueDate - DateTime.Today).Days <= 1);
            int pendingMinutes = StudyTasks.Where(task => !task.IsDone).Sum(task => ParseMinutes(task.EstimateText));
            int deepTasks = StudyTasks.Count(task => !task.IsDone && ParseMinutes(task.EstimateText) >= 45);
            int completionPercent = total == 0 ? 0 : (int)Math.Round((double)completed / total * 100.0);

            CompletedTasksText = completed + "/" + total;
            PendingTasksText = pending.ToString(CultureInfo.InvariantCulture);
            UrgentTasksText = urgent.ToString(CultureInfo.InvariantCulture);
            PlannedMinutesText = pendingMinutes + " دقيقة";
            OverallProgressText = completionPercent + "%";
            OverallProgressSubtitle = pending == 0
                ? "أقفلت القائمة كاملة"
                : "أنجزت " + completed + " من " + total + " مهام";
            OverallProgressWidth = 228.0 * completionPercent / 100.0;
            IsTaskListEmpty = total == 0;

            if (pending == 0)
            {
                TaskListSubtitle = "المشهد نظيف. إمّا تضيف مراجعة قصيرة أو تأخذ استراحة وأنت رافع الرأس.";
                DailyMissionText = "كل شيء مقفول. خلي الإغلاق الختامي مراجعة واحدة لأضعف مادة فقط.";
            }
            else if (urgent >= 2)
            {
                TaskListSubtitle = "عندك ضغط حقيقي. لا تفتح مسارات جديدة قبل إسقاط العاجل.";
                DailyMissionText = "أسقط " + urgent + " مهمة قريبة الموعد، وخصوصًا " + deepTasks + " مهمة ثقيلة قبل آخر اليوم.";
            }
            else
            {
                TaskListSubtitle = "القائمة تحت السيطرة، لكن الحسم الآن أهم من الكمال.";
                DailyMissionText = "أغلق " + Math.Min(2, pending) + " مهمة فقط اليوم. الجودة بعد الإغلاق، لا قبله.";
            }

            TaskHeatText = urgent >= 3 ? "ضغط عالي" : urgent >= 1 ? "ضغط محسوب" : "إيقاع نظيف";
        }

        private void UpdateStudyInsights()
        {
            SubjectOverview strongestSubject = Subjects.OrderByDescending(subject => subject.CompletionPercent).FirstOrDefault();
            SubjectOverview weakestSubject = Subjects.OrderBy(subject => subject.CompletionPercent).FirstOrDefault();

            if (strongestSubject != null)
            {
                WinningSubjectText = strongestSubject.Name + " " + strongestSubject.CompletionLabel;
            }

            if (weakestSubject != null)
            {
                RecoverySubjectText = weakestSubject.Name + " تحتاج دفعة قصيرة اليوم قبل أن تتمدد.";
            }

            StudySession nextSession = TodaySessions
                .Where(session => session.StartAt >= DateTime.Now.AddMinutes(-10))
                .OrderBy(session => session.StartAt)
                .FirstOrDefault();

            if (nextSession == null)
            {
                NextSessionText = "لا توجد جلسة قادمة. اصنع جلسة إغلاق خفيفة الآن.";
                FocusLaneText = "20 دقيقة مراجعة سريعة ثم قفل اليوم";
            }
            else
            {
                NextSessionText = nextSession.Subject + " | " + nextSession.TimeRange;
                FocusLaneText = nextSession.TimeRange + " - " + nextSession.SessionLabel;
            }

            int completed = StudyTasks.Count(task => task.IsDone);
            int totalProgress = Subjects.Any() ? (int)Math.Round(Subjects.Average(subject => subject.CompletionPercent)) : 0;
            int streak = 5 + completed + (totalProgress / 18);

            StudyStreakText = streak + " أيام";
            MomentumText = totalProgress >= 80
                ? "زخمك قوي. نفّذ الثقيل أولًا لأنك أصلًا داخل الإيقاع."
                : "الزخم موجود، لكن يحتاج ضربة نظيفة على أضعف جبهة اليوم.";
        }

        private void UpdateRecommendation()
        {
            List<StudyTask> pendingTasks = StudyTasks
                .Where(task => !task.IsDone)
                .OrderBy(task => task.DueDate)
                .ThenByDescending(task => task.PriorityWeight)
                .ThenBy(task => task.ConfidencePercent)
                .ToList();

            if (pendingTasks.Count == 0)
            {
                SmartRecommendationTitle = "الميدان نظيف";
                SmartRecommendationBody = "اعمل مراجعة قصيرة 15-20 دقيقة للمادة الأضعف فقط، ثم اخرج من اليوم وأنت قافله صح.";
                return;
            }

            StudyTask chosenTask = pendingTasks[_recommendationOffset % pendingTasks.Count];
            SmartRecommendationTitle = chosenTask.Title;
            SmartRecommendationBody =
                "ابدأ بـ " + chosenTask.Subject +
                " لمدة " + chosenTask.EstimateText +
                ". سَوِّها كالتالي: " + chosenTask.StudyMode +
                ". " + chosenTask.DueText +
                "، ونبرة المهمة " + chosenTask.TaskLaneText +
                "، وثقتك الحالية " + chosenTask.ConfidenceText + ".";
        }

        private static int ResolvePriorityWeight(string priority)
        {
            if (priority == "عاجل")
            {
                return 3;
            }

            if (priority == "مهم")
            {
                return 2;
            }

            return 1;
        }

        private static DateTime ResolveDueDate(string priority)
        {
            if (priority == "عاجل")
            {
                return DateTime.Today;
            }

            if (priority == "مهم")
            {
                return DateTime.Today.AddDays(1);
            }

            return DateTime.Today.AddDays(3);
        }

        private static string BuildStudyMode(string subject)
        {
            if (string.IsNullOrEmpty(subject)) return "جلسة دراسة مركزة";

            switch (subject)
            {
                case "الرياضيات":
                case "الفيزياء":
                case "الكيمياء":
                    return "حل تمارين ثم مراجعة الأخطاء فوراً";
                case "الأحياء":
                case "التاريخ":
                case "الجغرافيا":
                    return "استرجاع نشط (Active Recall) بدون النظر للكتاب";
                case "اللغة الإنجليزية":
                case "العربية":
                case "اللغات":
                    return "تمرين كتابي وممارسة نطق سريعة";
                case "علوم الحاسب":
                case "البرمجة":
                    return "جلسة عمل عميق (Deep Work) على الكود";
                default:
                    return "دراسة عميقة بتركيز 100% بدون مقاطعات";
            }
        }

        private static Brush ResolveAccentBrush(string subject)
        {
            if (string.IsNullOrEmpty(subject)) return BrushFromHex("#FF475569");

            switch (subject)
            {
                case "الرياضيات": return BrushFromHex("#FF14B8A6");
                case "الأحياء": return BrushFromHex("#FF0EA5E9");
                case "اللغة الإنجليزية": return BrushFromHex("#FFF97316");
                case "علوم الحاسب": return BrushFromHex("#FF2563EB");
                case "الفيزياء": return BrushFromHex("#FF8B5CF6");
                case "الكيمياء": return BrushFromHex("#FFEF4444");
                default:
                    // Generate a color based on the subject name to keep it consistent
                    return GetColorForSubject(subject);
            }
        }

        private static Brush ResolveSoftAccentBrush(string subject)
        {
            if (string.IsNullOrEmpty(subject)) return BrushFromHex("#FFF3F4F6");

            switch (subject)
            {
                case "الرياضيات": return BrushFromHex("#FFE8F8F6");
                case "الأحياء": return BrushFromHex("#FFEAF8FD");
                case "اللغة الإنجليزية": return BrushFromHex("#FFFFF1E7");
                case "علوم الحاسب": return BrushFromHex("#FFEAF0FF");
                case "الفيزياء": return BrushFromHex("#FFF5F3FF");
                case "الكيمياء": return BrushFromHex("#FFFEF2F2");
                default:
                    return BrushFromHex("#FFF3F4F6");
            }
        }

        private static Brush GetColorForSubject(string subject)
        {
            string[] professionalColors = { "#FF14B8A6", "#FF0EA5E9", "#FFF97316", "#FF2563EB", "#FF8B5CF6", "#FFEF4444", "#FFEC4899", "#FF6366F1" };
            int hash = Math.Abs(subject.GetHashCode());
            return BrushFromHex(professionalColors[hash % professionalColors.Length]);
        }

        private static int ParseMinutes(string text)
        {
            string digits = new string((text ?? string.Empty).Where(char.IsDigit).ToArray());
            int minutes;
            return int.TryParse(digits, out minutes) ? minutes : 0;
        }

        private static DateTime RoundToNextHalfHour(DateTime value)
        {
            DateTime trimmed = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
            int remainder = trimmed.Minute % 30;
            int minutesToAdd = remainder == 0 ? 0 : 30 - remainder;
            DateTime rounded = trimmed.AddMinutes(minutesToAdd);
            if (rounded <= value)
            {
                rounded = rounded.AddMinutes(30);
            }

            return rounded;
        }

        private static Brush BrushFromHex(string hex)
        {
            return (Brush)new BrushConverter().ConvertFromString(hex);
        }

        private static CultureInfo CreateArabicCulture()
        {
            CultureInfo culture = new CultureInfo("ar-EG");
            culture.DateTimeFormat.Calendar = new GregorianCalendar();
            return culture;
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class SubjectOverview
    {
        public string Name { get; set; }

        public double HoursStudied { get; set; }

        public double HoursTarget { get; set; }

        public int CompletionPercent { get; set; }

        public string StatusText { get; set; }

        public Brush AccentBrush { get; set; }

        public Brush SoftAccentBrush { get; set; }

        public string CompletionLabel
        {
            get { return CompletionPercent + "%"; }
        }

        public string HoursSummary
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.0} من {1:0.0} ساعة",
                    HoursStudied,
                    HoursTarget);
            }
        }

        public double ProgressWidth
        {
            get { return 250.0 * CompletionPercent / 100.0; }
        }
    }

    public class StudySession
    {
        public string Subject { get; set; }

        public string Topic { get; set; }

        public DateTime StartAt { get; set; }

        public int DurationMinutes { get; set; }

        public Brush AccentBrush { get; set; }

        public Brush SoftAccentBrush { get; set; }

        public string SubjectInitial
        {
            get
            {
                return string.IsNullOrWhiteSpace(Subject) ? "د" : Subject.Trim().Substring(0, 1);
            }
        }

        public string TimeRange
        {
            get
            {
                return StartAt.ToString("HH:mm", CultureInfo.InvariantCulture) + " - " +
                       StartAt.AddMinutes(DurationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture);
            }
        }

        public string SessionLabel
        {
            get
            {
                if (DurationMinutes >= 50)
                {
                    return "Deep Focus";
                }

                if (DurationMinutes >= 35)
                {
                    return "Sharp Sprint";
                }

                return "Quick Review";
            }
        }
    }

    public class StudyTask : INotifyPropertyChanged
    {
        private bool _isDone;

        public string Title { get; set; }

        public string Subject { get; set; }

        public DateTime DueDate { get; set; }

        public string EstimateText { get; set; }

        public string StudyMode { get; set; }

        public int ConfidencePercent { get; set; }

        public int PriorityWeight { get; set; }

        public string PriorityLabel { get; set; }

        public Brush AccentBrush { get; set; }

        public Brush SoftAccentBrush { get; set; }

        public bool IsDone
        {
            get { return _isDone; }
            set
            {
                if (_isDone == value)
                {
                    return;
                }

                _isDone = value;
                OnPropertyChanged();
                OnPropertyChanged("StatusLabel");
                OnPropertyChanged("CardOpacity");
            }
        }

        public string SubjectInitial
        {
            get
            {
                return string.IsNullOrWhiteSpace(Subject) ? "د" : Subject.Trim().Substring(0, 1);
            }
        }

        public string DueText
        {
            get
            {
                int days = (DueDate.Date - DateTime.Today).Days;
                if (days <= 0)
                {
                    return "موعدها اليوم";
                }

                if (days == 1)
                {
                    return "موعدها غدًا";
                }

                return "بعد " + days + " أيام";
            }
        }

        public string ConfidenceText
        {
            get { return "ثقة " + ConfidencePercent + "%"; }
        }

        public string TaskLaneText
        {
            get
            {
                int minutes = ParseEstimateMinutes();
                if (PriorityWeight >= 3 || minutes >= 45)
                {
                    return "Deep Focus";
                }

                if (minutes <= 20)
                {
                    return "Quick Win";
                }

                return "Sharp Sprint";
            }
        }

        public string StatusLabel
        {
            get { return IsDone ? "مكتملة" : PriorityLabel; }
        }

        public double CardOpacity
        {
            get { return IsDone ? 0.58 : 1.0; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private int ParseEstimateMinutes()
        {
            string digits = new string((EstimateText ?? string.Empty).Where(char.IsDigit).ToArray());
            int minutes;
            return int.TryParse(digits, out minutes) ? minutes : 0;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
