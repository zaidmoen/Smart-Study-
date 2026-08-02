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
using System.Windows.Input;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private static readonly CultureInfo ArabicCulture = new CultureInfo("ar-EG") { DateTimeFormat = { Calendar = new GregorianCalendar() } };
        private static readonly string[] AccentPalette =
        {
            "#FF14B8A6",
            "#FF2563EB",
            "#FFF97316",
            "#FF8B5CF6",
            "#FFEF4444"
        };
        private static readonly string[] SoftAccentPalette =
        {
            "#FFE8F8F6",
            "#FFEFF6FF",
            "#FFFFF3E8",
            "#FFF5F0FF",
            "#FFFFEFEF"
        };
        private readonly DispatcherTimer _clockTimer;
        private readonly StorageService _storageService;

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
        private string _studentJoyTitle;
        private string _studentJoyBody;
        private string _studentRewardText;
        private string _studentCompanionText;
        private string _newTaskTitle;
        private string _selectedSubjectOption;
        private string _selectedDurationOption;
        private string _selectedPriorityOption;
        private bool _isTaskListEmpty;
        private string _totalStudyHoursText;
        private string _completionRateText;
        private string _remainingWorkText;
        private string _averageFocusText;
        private string _urgentWorkText;
        private string _focusScoreText;
        private double _focusScoreWidth;
        private string _productivityGradeText;
        private string _primaryInsightTitle;
        private string _primaryInsightBody;
        private string _bestNextActionText;
        private string _workloadBalanceText;
        private string _subjectAnalyticsSubtitle;
        private string _storageHealthText;

        private object _currentView;

        public MainViewModel()
        {
            Subjects = new ObservableCollection<SubjectOverview>();
            TodaySessions = new ObservableCollection<StudySession>();
            StudyTasks = new ObservableCollection<StudyTask>();
            PriorityOptions = new ObservableCollection<string> { "عادي", "مهم", "عاجل" };
            
            _storageService = new StorageService();
            
            LoadData();
            ConfigureTaskSorting();
            ResetQuickAddFields();
            RefreshDashboard();

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _clockTimer.Tick += (s, e) => RefreshDashboard();
            _clockTimer.Start();

            AddTaskCommand = new RelayCommand(o => AddTask());
            GenerateSmartPlanCommand = new RelayCommand(o => { _recommendationOffset++; RefreshDashboard(); });
            StartFocusCommand = new RelayCommand(o => StartFocus());
            DeleteTaskCommand = new RelayCommand(DeleteTask, o => o is StudyTask);
            ClearCompletedCommand = new RelayCommand(o => ClearCompletedTasks(), o => StudyTasks.Any(task => task.IsDone));
            NavigateCommand = new RelayCommand(o => CurrentView = o);

            CurrentView = "Dashboard"; // Initial view
        }

        public object CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }
        public ICommand NavigateCommand { get; }

        public ObservableCollection<SubjectOverview> Subjects { get; }
        public ObservableCollection<StudySession> TodaySessions { get; }
        public ObservableCollection<StudyTask> StudyTasks { get; }
        public ObservableCollection<string> PriorityOptions { get; }

        public ICommand AddTaskCommand { get; }
        public ICommand GenerateSmartPlanCommand { get; }
        public ICommand StartFocusCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ClearCompletedCommand { get; }

        // Properties (Getters/Setters)
        public string CurrentDateText { get => _currentDateText; set => SetProperty(ref _currentDateText, value); }
        public string CurrentTimeText { get => _currentTimeText; set => SetProperty(ref _currentTimeText, value); }
        public string SmartRecommendationTitle { get => _smartRecommendationTitle; set => SetProperty(ref _smartRecommendationTitle, value); }
        public string SmartRecommendationBody { get => _smartRecommendationBody; set => SetProperty(ref _smartRecommendationBody, value); }
        public string CompletedTasksText { get => _completedTasksText; set => SetProperty(ref _completedTasksText, value); }
        public string PendingTasksText { get => _pendingTasksText; set => SetProperty(ref _pendingTasksText, value); }
        public string UrgentTasksText { get => _urgentTasksText; set => SetProperty(ref _urgentTasksText, value); }
        public string PlannedMinutesText { get => _plannedMinutesText; set => SetProperty(ref _plannedMinutesText, value); }
        public string TaskListSubtitle { get => _taskListSubtitle; set => SetProperty(ref _taskListSubtitle, value); }
        public string DailyMissionText { get => _dailyMissionText; set => SetProperty(ref _dailyMissionText, value); }
        public string OverallProgressText { get => _overallProgressText; set => SetProperty(ref _overallProgressText, value); }
        public string OverallProgressSubtitle { get => _overallProgressSubtitle; set => SetProperty(ref _overallProgressSubtitle, value); }
        public double OverallProgressWidth { get => _overallProgressWidth; set => SetProperty(ref _overallProgressWidth, value); }
        public string StudyStreakText { get => _studyStreakText; set => SetProperty(ref _studyStreakText, value); }
        public string MomentumText { get => _momentumText; set => SetProperty(ref _momentumText, value); }
        public string FocusLaneText { get => _focusLaneText; set => SetProperty(ref _focusLaneText, value); }
        public string NextSessionText { get => _nextSessionText; set => SetProperty(ref _nextSessionText, value); }
        public string TaskHeatText { get => _taskHeatText; set => SetProperty(ref _taskHeatText, value); }
        public string WinningSubjectText { get => _winningSubjectText; set => SetProperty(ref _winningSubjectText, value); }
        public string RecoverySubjectText { get => _recoverySubjectText; set => SetProperty(ref _recoverySubjectText, value); }
        public string StudentJoyTitle { get => _studentJoyTitle; set => SetProperty(ref _studentJoyTitle, value); }
        public string StudentJoyBody { get => _studentJoyBody; set => SetProperty(ref _studentJoyBody, value); }
        public string StudentRewardText { get => _studentRewardText; set => SetProperty(ref _studentRewardText, value); }
        public string StudentCompanionText { get => _studentCompanionText; set => SetProperty(ref _studentCompanionText, value); }
        public string NewTaskTitle { get => _newTaskTitle; set => SetProperty(ref _newTaskTitle, value); }
        public string SelectedSubjectOption { get => _selectedSubjectOption; set => SetProperty(ref _selectedSubjectOption, value); }
        public string SelectedDurationOption { get => _selectedDurationOption; set => SetProperty(ref _selectedDurationOption, value); }
        public string SelectedPriorityOption { get => _selectedPriorityOption; set => SetProperty(ref _selectedPriorityOption, value); }
        public bool IsTaskListEmpty { get => _isTaskListEmpty; set => SetProperty(ref _isTaskListEmpty, value); }
        public string TotalStudyHoursText { get => _totalStudyHoursText; set => SetProperty(ref _totalStudyHoursText, value); }
        public string CompletionRateText { get => _completionRateText; set => SetProperty(ref _completionRateText, value); }
        public string RemainingWorkText { get => _remainingWorkText; set => SetProperty(ref _remainingWorkText, value); }
        public string AverageFocusText { get => _averageFocusText; set => SetProperty(ref _averageFocusText, value); }
        public string UrgentWorkText { get => _urgentWorkText; set => SetProperty(ref _urgentWorkText, value); }
        public string FocusScoreText { get => _focusScoreText; set => SetProperty(ref _focusScoreText, value); }
        public double FocusScoreWidth { get => _focusScoreWidth; set => SetProperty(ref _focusScoreWidth, value); }
        public string ProductivityGradeText { get => _productivityGradeText; set => SetProperty(ref _productivityGradeText, value); }
        public string PrimaryInsightTitle { get => _primaryInsightTitle; set => SetProperty(ref _primaryInsightTitle, value); }
        public string PrimaryInsightBody { get => _primaryInsightBody; set => SetProperty(ref _primaryInsightBody, value); }
        public string BestNextActionText { get => _bestNextActionText; set => SetProperty(ref _bestNextActionText, value); }
        public string WorkloadBalanceText { get => _workloadBalanceText; set => SetProperty(ref _workloadBalanceText, value); }
        public string SubjectAnalyticsSubtitle { get => _subjectAnalyticsSubtitle; set => SetProperty(ref _subjectAnalyticsSubtitle, value); }
        public string StorageHealthText { get => _storageHealthText; set => SetProperty(ref _storageHealthText, value); }

        private void AddTask()
        {
            string title = (NewTaskTitle ?? string.Empty).Trim();
            string subject = (SelectedSubjectOption ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title)) return;

            if (string.IsNullOrWhiteSpace(subject)) subject = "عام";

            string durationText = SelectedDurationOption;
            if (!durationText.Contains("دقيقة") && durationText.Any(char.IsDigit)) durationText += " دقيقة";
            else if (string.IsNullOrWhiteSpace(durationText)) durationText = "30 دقيقة";

            var task = new StudyTask
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
            };

            AttachTaskHandlers(task);
            StudyTasks.Add(task);
            SaveData();
            ResetQuickAddFields();
            RefreshDashboard();
            CommandManager.InvalidateRequerySuggested();
        }

        private void DeleteTask(object parameter)
        {
            var task = parameter as StudyTask;
            if (task == null) return;

            if (StudyTasks.Remove(task))
            {
                task.PropertyChanged -= StudyTask_PropertyChanged;
                SaveData();
                RefreshDashboard();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ClearCompletedTasks()
        {
            var completedTasks = StudyTasks.Where(task => task.IsDone).ToList();
            foreach (var task in completedTasks)
            {
                task.PropertyChanged -= StudyTask_PropertyChanged;
                StudyTasks.Remove(task);
            }

            SaveData();
            RefreshDashboard();
            CommandManager.InvalidateRequerySuggested();
        }

        private void StartFocus()
        {
             string message = SmartRecommendationTitle
                 + Environment.NewLine + Environment.NewLine
                 + SmartRecommendationBody
                 + Environment.NewLine + Environment.NewLine
                 + StudentCompanionText
                 + Environment.NewLine
                 + StudentRewardText;
             MessageBox.Show(message, "غرفة التركيز");
        }

        private void LoadData()
        {
            var tasks = _storageService.LoadTasks();
            foreach (var t in tasks)
            {
                if (string.IsNullOrWhiteSpace(t.StudyMode)) t.StudyMode = BuildStudyMode(t.Subject);
                if (string.IsNullOrWhiteSpace(t.PriorityLabel)) t.PriorityLabel = "عادي";
                if (t.PriorityWeight <= 0) t.PriorityWeight = ResolvePriorityWeight(t.PriorityLabel);
                if (string.IsNullOrWhiteSpace(t.EstimateText)) t.EstimateText = "30 دقيقة";
                if (t.ConfidencePercent <= 0) t.ConfidencePercent = 70;

                t.AccentBrush = ResolveAccentBrush(t.Subject);
                t.SoftAccentBrush = ResolveSoftAccentBrush(t.Subject);
                AttachTaskHandlers(t);
                StudyTasks.Add(t);
            }

            StorageHealthText = string.IsNullOrWhiteSpace(_storageService.LastError)
                ? "الحفظ التلقائي جاهز"
                : "تم تجاوز ملف حفظ غير صالح";
        }

        private void SaveData()
        {
            bool saved = _storageService.SaveTasks(StudyTasks.ToList());
            StorageHealthText = saved ? "آخر حفظ تم بنجاح" : "تعذر الحفظ: " + _storageService.LastError;
        }

        private void AttachTaskHandlers(StudyTask task)
        {
            task.PropertyChanged -= StudyTask_PropertyChanged;
            task.PropertyChanged += StudyTask_PropertyChanged;
        }

        private void StudyTask_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IsDone") return;

            SaveData();
            RefreshDashboard();
            CommandManager.InvalidateRequerySuggested();
        }

        private void RefreshDashboard()
        {
            UpdateClock();
            UpdateTaskSummary();
            UpdateStudyInsights();
            UpdateDashboardAnalytics();
            UpdateRecommendation();
            UpdateStudentBoost();
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
            int completionPercent = total == 0 ? 0 : (int)Math.Round((double)completed / total * 100.0);

            CompletedTasksText = completed + "/" + total;
            PendingTasksText = pending.ToString();
            UrgentTasksText = urgent.ToString();
            PlannedMinutesText = pendingMinutes + " دقيقة";
            OverallProgressText = completionPercent + "%";
            OverallProgressSubtitle = pending == 0 ? "أقفلت القائمة كاملة" : "أنجزت " + completed + " من " + total + " مهام";
            OverallProgressWidth = 228.0 * completionPercent / 100.0;
            IsTaskListEmpty = total == 0;

            if (pending == 0) TaskListSubtitle = "المشهد نظيف.";
            else if (urgent >= 2) TaskListSubtitle = "عندك ضغط حقيقي.";
            else TaskListSubtitle = "القائمة تحت السيطرة.";

            DailyMissionText = pending == 0 ? "كل شيء مقفول." : "أغلق " + Math.Min(2, pending) + " مهمة اليوم.";
            TaskHeatText = urgent >= 3 ? "ضغط عالي" : urgent >= 1 ? "ضغط محسوب" : "إيقاع نظيف";
        }

        private void UpdateDashboardAnalytics()
        {
            int total = StudyTasks.Count;
            int completed = StudyTasks.Count(task => task.IsDone);
            int pending = total - completed;
            int urgent = StudyTasks.Count(task => !task.IsDone && (task.DueDate - DateTime.Today).Days <= 1);
            int totalMinutes = StudyTasks.Sum(task => NormalizeDuration(ParseMinutes(task.EstimateText)));
            int completedMinutes = StudyTasks.Where(task => task.IsDone).Sum(task => NormalizeDuration(ParseMinutes(task.EstimateText)));
            int pendingMinutes = Math.Max(0, totalMinutes - completedMinutes);
            int completionPercent = total == 0 ? 0 : (int)Math.Round((double)completed / total * 100.0);
            int averageMinutes = total == 0 ? 30 : Math.Max(20, totalMinutes / total);
            int focusScore = CalculateFocusScore(completionPercent, urgent, pendingMinutes, Subjects.Count, total);
            var pendingTasks = GetOrderedPendingTasks();
            StudyTask nextTask = pendingTasks.FirstOrDefault();

            TotalStudyHoursText = FormatMinutes(completedMinutes);
            CompletionRateText = completionPercent + "%";
            RemainingWorkText = FormatMinutes(pendingMinutes);
            AverageFocusText = averageMinutes + " دقيقة";
            UrgentWorkText = urgent == 0 ? "لا يوجد" : urgent + " عاجلة";
            FocusScoreText = total == 0 ? "--" : focusScore + "/100";
            FocusScoreWidth = total == 0 ? 0 : 250.0 * focusScore / 100.0;
            WorkloadBalanceText = BuildWorkloadBalanceText(pendingMinutes, urgent, pending);
            SubjectAnalyticsSubtitle = Subjects.Count == 0
                ? "لا توجد مواد بعد. أضف أول مهمة حتى تظهر خريطة الدراسة."
                : Subjects.Count + " مواد نشطة | " + FormatMinutes(totalMinutes) + " في الخطة";

            if (total == 0)
            {
                ProductivityGradeText = "جاهز للبداية";
                PrimaryInsightTitle = "ابنِ أول خطة صغيرة";
                PrimaryInsightBody = "أضف مهمة واحدة واضحة مع مدة تقديرية. بعدها ستتحول اللوحة إلى نظام قرار يومي بدل قائمة عادية.";
                BestNextActionText = "أضف مهمة مدتها 25-30 دقيقة في مادة واحدة.";
                return;
            }

            if (pending == 0)
            {
                ProductivityGradeText = "ممتاز";
                PrimaryInsightTitle = "اليوم مغلق بنظافة";
                PrimaryInsightBody = "كل المهام مكتملة. القيمة الآن في الحفاظ على السلسلة: راجع ما أنجزته وخطط لمهمة صغيرة للغد.";
                BestNextActionText = "اكتب مهمة الغد الأولى قبل إغلاق التطبيق.";
                return;
            }

            ProductivityGradeText = focusScore >= 85 ? "سيطرة ممتازة" : focusScore >= 65 ? "إيقاع جيد" : focusScore >= 45 ? "يحتاج ترتيب" : "ضغط مرتفع";
            PrimaryInsightTitle = urgent >= 2 ? "ابدأ بالعاجل قبل أن يكبر" : pendingMinutes >= 180 ? "قسّم اليوم إلى جولات" : "اختيارك القادم واضح";
            PrimaryInsightBody = BuildPrimaryInsightBody(completionPercent, urgent, pendingMinutes, pending);
            BestNextActionText = nextTask == null
                ? "خذ راحة قصيرة ثم راجع الخطة."
                : "ابدأ الآن: " + NormalizeSubject(nextTask.Subject) + " | " + nextTask.Title + " | " + NormalizeDuration(ParseMinutes(nextTask.EstimateText)) + " دقيقة";
        }

        private void UpdateStudyInsights()
        {
            UpdateSubjectOverview();
            UpdateTodaySessions();

            var pendingTasks = GetOrderedPendingTasks();
            int completedCount = StudyTasks.Count(task => task.IsDone);
            int pendingMinutes = pendingTasks.Sum(task => ParseMinutes(task.EstimateText));

            if (Subjects.Count == 0)
            {
                WinningSubjectText = "أضف أول مادة";
                RecoverySubjectText = "ابدأ بمادة تحبها حتى تدخل الجو وتكسب أول دفعة.";
            }
            else
            {
                SubjectOverview winningSubject = Subjects
                    .OrderByDescending(subject => subject.CompletionPercent)
                    .ThenByDescending(subject => subject.HoursStudied)
                    .First();
                WinningSubjectText = winningSubject.Name + " " + winningSubject.CompletionLabel;

                SubjectOverview recoverySubject = Subjects
                    .OrderBy(subject => subject.CompletionPercent)
                    .ThenByDescending(subject => Math.Max(0, subject.HoursTarget - subject.HoursStudied))
                    .First();
                RecoverySubjectText = recoverySubject.CompletionPercent >= 85
                    ? "المواد ماشية بنَفَس ممتاز، حافظ على اللمسة اليومية."
                    : "مادة الإنقاذ اليوم: " + recoverySubject.Name + ". جلسة قصيرة تكفي لتعديل المزاج.";
            }

            if (pendingTasks.Count == 0)
            {
                NextSessionText = "لا توجد جلسة قادمة، خذ راحة مستحقة.";
                FocusLaneText = "10 دقائق ترتيب خفيف ثم كافئ نفسك.";
                StudyStreakText = completedCount == 0 ? "جاهز لأول دفعة" : completedCount + " مهام مكتملة";
                MomentumText = completedCount == 0
                    ? "ابدأ بأبسط مهمة، والباقي سيصير أخف."
                    : "أنهيت اليوم بشكل محترم، استمتع بالإحساس.";
                return;
            }

            StudyTask nextTask = pendingTasks[0];
            int nextDuration = NormalizeDuration(ParseMinutes(nextTask.EstimateText));

            NextSessionText = NormalizeSubject(nextTask.Subject) + " | " + nextTask.Title + " لمدة " + nextDuration + " دقيقة";
            FocusLaneText = BuildFocusLane(nextTask, pendingMinutes, pendingTasks.Count);
            StudyStreakText = completedCount == 0 ? "دفعة البداية" : (completedCount + 1) + " دفعات ثقة";
            MomentumText = BuildMomentumText(completedCount, pendingTasks.Count);
        }

        private void UpdateRecommendation()
        {
            var pendingTasks = GetOrderedPendingTasks();
            if (pendingTasks.Count == 0)
            {
                SmartRecommendationTitle = "الميدان نظيف";
                SmartRecommendationBody = "استرخِ قليلاً.";
                return;
            }
            var chosen = pendingTasks[_recommendationOffset % pendingTasks.Count];
            SmartRecommendationTitle = chosen.Title;
            SmartRecommendationBody = "ابدأ بـ " + chosen.Subject + " لمدة " + chosen.EstimateText;
        }

        private void UpdateStudentBoost()
        {
            var pendingTasks = GetOrderedPendingTasks();
            int completedCount = StudyTasks.Count(task => task.IsDone);
            int pendingMinutes = pendingTasks.Sum(task => ParseMinutes(task.EstimateText));

            if (pendingTasks.Count == 0)
            {
                StudentJoyTitle = "اليوم مقفول";
                StudentJoyBody = "خلصت المطلوب. خذ ربع ساعة بدون تأنيب: مشروب تحبه، حلقة قصيرة، أو تمشية بسيطة.";
                StudentRewardText = "المكافأة: راحة 15 دقيقة";
                StudentCompanionText = "الجو: راجع إنجازك وابتسم";
                return;
            }

            StudyTask chosen = pendingTasks[_recommendationOffset % pendingTasks.Count];

            StudentJoyTitle = chosen.PriorityWeight >= 3
                ? "إنقاذ سريع"
                : completedCount == 0
                    ? "بداية تشرح الصدر"
                    : pendingTasks.Count == 1
                        ? "قفلة اليوم"
                        : "حافظ على النفس";
            StudentJoyBody = BuildStudentJoyBody(chosen, completedCount, pendingMinutes, pendingTasks.Count);
            StudentRewardText = BuildRewardIdea(chosen, completedCount);
            StudentCompanionText = BuildCompanionText(chosen);
        }

        private void UpdateSubjectOverview()
        {
            Subjects.Clear();

            var groupedTasks = StudyTasks
                .GroupBy(task => NormalizeSubject(task.Subject))
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .ToList();
            int allPlannedMinutes = groupedTasks.Sum(group => group.Sum(task => NormalizeDuration(ParseMinutes(task.EstimateText))));

            foreach (var group in groupedTasks)
            {
                int totalTasks = group.Count();
                int completedTasks = group.Count(task => task.IsDone);
                int totalMinutes = group.Sum(task => NormalizeDuration(ParseMinutes(task.EstimateText)));
                int completedMinutes = group.Where(task => task.IsDone).Sum(task => NormalizeDuration(ParseMinutes(task.EstimateText)));
                int completionPercent = totalTasks == 0
                    ? 0
                    : (int)Math.Round((double)completedTasks / totalTasks * 100.0);
                int remainingMinutes = Math.Max(0, totalMinutes - completedMinutes);
                int sharePercent = allPlannedMinutes == 0
                    ? 0
                    : (int)Math.Round((double)totalMinutes / allPlannedMinutes * 100.0);

                Subjects.Add(new SubjectOverview
                {
                    Name = group.Key,
                    HoursStudied = completedMinutes / 60.0,
                    HoursTarget = Math.Max(30, totalMinutes) / 60.0,
                    CompletedMinutes = completedMinutes,
                    TotalMinutes = totalMinutes,
                    RemainingMinutes = remainingMinutes,
                    CompletionPercent = completionPercent,
                    SharePercent = sharePercent,
                    StatusText = BuildSubjectStatus(completedTasks, totalTasks, remainingMinutes, completionPercent),
                    AccentBrush = ResolveAccentBrush(group.Key),
                    SoftAccentBrush = ResolveSoftAccentBrush(group.Key)
                });
            }
        }

        private void UpdateTodaySessions()
        {
            TodaySessions.Clear();

            var plannedTasks = GetOrderedPendingTasks()
                .Take(3)
                .ToList();

            DateTime startAt = RoundToNextQuarter(DateTime.Now.AddMinutes(10));

            foreach (var task in plannedTasks)
            {
                int duration = NormalizeDuration(ParseMinutes(task.EstimateText));

                TodaySessions.Add(new StudySession
                {
                    Subject = NormalizeSubject(task.Subject),
                    Topic = BuildSessionTopic(task),
                    StartAt = startAt,
                    DurationMinutes = duration,
                    AccentBrush = ResolveAccentBrush(task.Subject),
                    SoftAccentBrush = ResolveSoftAccentBrush(task.Subject)
                });

                startAt = startAt.AddMinutes(duration + 10);
            }
        }

        // Helper methods (Moved from MainWindow.xaml.cs)
        private List<StudyTask> GetOrderedPendingTasks()
        {
            return StudyTasks.Where(task => !task.IsDone)
                .OrderByDescending(task => task.PriorityWeight)
                .ThenBy(task => task.DueDate)
                .ThenByDescending(task => ParseMinutes(task.EstimateText))
                .ToList();
        }

        private static int CalculateFocusScore(int completionPercent, int urgentCount, int pendingMinutes, int subjectCount, int totalTasks)
        {
            if (totalTasks == 0) return 0;

            int completionScore = (int)Math.Round(completionPercent * 0.55);
            int urgencyScore = urgentCount == 0 ? 25 : urgentCount == 1 ? 16 : urgentCount == 2 ? 8 : 2;
            int workloadScore = pendingMinutes <= 60 ? 15 : pendingMinutes <= 150 ? 11 : pendingMinutes <= 270 ? 6 : 2;
            int diversityScore = subjectCount <= 1 ? 3 : subjectCount <= 4 ? 5 : 4;

            return Math.Max(0, Math.Min(100, completionScore + urgencyScore + workloadScore + diversityScore));
        }

        private static string BuildWorkloadBalanceText(int pendingMinutes, int urgentCount, int pendingCount)
        {
            if (pendingCount == 0) return "لا يوجد حمل متبقٍ اليوم.";
            if (urgentCount >= 3) return "ابدأ بالعاجل فقط، واترك التحسينات لوقت لاحق.";
            if (pendingMinutes >= 240) return "الحمل كبير. قسمه إلى ثلاث جولات ولا تفتح مهام جديدة.";
            if (pendingMinutes >= 120) return "الحمل متوسط. جولتان مركزتان تكفيان لقلب اليوم.";

            return "الحمل خفيف. مهمة واحدة الآن ترفع المؤشر بسرعة.";
        }

        private static string BuildPrimaryInsightBody(int completionPercent, int urgentCount, int pendingMinutes, int pendingCount)
        {
            if (urgentCount >= 2)
            {
                return "عندك " + urgentCount + " مهام عاجلة. أغلق أقصر مهمة عاجلة أولا حتى ينخفض الضغط النفسي بسرعة.";
            }

            if (completionPercent >= 70)
            {
                return "أنت قريب من إغلاق اليوم. لا توسع الخطة الآن، اختر مهمة واحدة فقط وأنهِها.";
            }

            if (pendingMinutes >= 180)
            {
                return "الخطة ثقيلة على جلسة واحدة. الأفضل تحويلها إلى جولات 25-30 دقيقة مع استراحات قصيرة.";
            }

            return "باقي " + pendingCount + " مهام. القرار الأفضل هو بدء المهمة الأعلى أولوية بدل إعادة ترتيب القائمة.";
        }

        private static int ResolvePriorityWeight(string p) => p == "عاجل" ? 3 : p == "مهم" ? 2 : 1;
        private static DateTime ResolveDueDate(string p) => p == "عاجل" ? DateTime.Today : p == "مهم" ? DateTime.Today.AddDays(1) : DateTime.Today.AddDays(3);
        private static string BuildStudyMode(string s)
        {
            string subject = NormalizeSubject(s);

            if (ContainsAny(subject, "رياض", "Math", "جبر", "هندسة")) return "حل مسائل ثم مراجعة الأخطاء";
            if (ContainsAny(subject, "فيز", "كيمي", "أحياء", "علوم")) return "مفاهيم مركزة + أسئلة سريعة";
            if (ContainsAny(subject, "عرب", "إنج", "لغة", "فرنسي")) return "قراءة مركزة + تثبيت الأفكار";

            return "جولة تركيز قصيرة ثم تلخيص سريع";
        }
        private static Brush ResolveAccentBrush(string s)
        {
            int paletteIndex = GetSubjectPaletteIndex(s);
            return CreateBrush(AccentPalette[paletteIndex]);
        }
        private static Brush ResolveSoftAccentBrush(string s)
        {
            int paletteIndex = GetSubjectPaletteIndex(s);
            return CreateBrush(SoftAccentPalette[paletteIndex]);
        }
        private static int ParseMinutes(string t) => int.TryParse(new string((t ?? "").Where(char.IsDigit).ToArray()), out int m) ? m : 0;
        private static int NormalizeDuration(int minutes) => minutes <= 0 ? 30 : Math.Max(20, Math.Min(60, minutes));

        private static string FormatMinutes(int minutes)
        {
            if (minutes <= 0) return "0 دقيقة";
            if (minutes < 60) return minutes + " دقيقة";

            int hours = minutes / 60;
            int rest = minutes % 60;
            return rest == 0 ? hours + " ساعة" : hours + "س " + rest + "د";
        }

        private static string BuildSubjectStatus(int completedTasks, int totalTasks, int remainingMinutes, int completionPercent)
        {
            if (totalTasks == 0) return "لا توجد مهام بعد.";
            if (completedTasks == totalTasks) return "المادة مقفولة اليوم، ممتاز.";
            if (completionPercent >= 70) return "قريبة من القفل. المتبقي حوالي " + remainingMinutes + " دقيقة.";
            if (completionPercent >= 35) return completedTasks + "/" + totalTasks + " مهام منجزة، بقي دفعة لطيفة.";
            return "ابدأها بجلسة خفيفة، المتبقي حوالي " + remainingMinutes + " دقيقة.";
        }

        private static string BuildSessionTopic(StudyTask task)
        {
            string subject = NormalizeSubject(task.Subject);
            return task.Title + " | " + subject + " | " + BuildStudyMode(subject);
        }

        private static string BuildFocusLane(StudyTask task, int pendingMinutes, int pendingTaskCount)
        {
            int duration = NormalizeDuration(ParseMinutes(task.EstimateText));

            if (task.PriorityWeight >= 3) return "25 دقيقة على " + NormalizeSubject(task.Subject) + " ثم 5 دقائق تنفّس.";
            if (pendingMinutes >= 150) return "اليوم مزدحم: جولتان 30 دقيقة ثم استراحة 10 دقائق.";
            if (pendingTaskCount == 1) return "هذه القفلة الأخيرة، أنهها ثم خذ مكافأتك.";

            return duration + " دقيقة تركيز على " + task.Title + " ثم مراجعة سريعة.";
        }

        private static string BuildMomentumText(int completedCount, int pendingCount)
        {
            if (completedCount == 0) return "ابدأ بجولة واحدة فقط، والباقي سيصير أخف.";
            if (completedCount >= pendingCount) return "أنت سابق القائمة، كمّل على نفس الإيقاع.";

            return "فيه شغل انقفل فعلًا، باقي دفعة لطيفة وتروق.";
        }

        private static string BuildStudentJoyBody(StudyTask task, int completedCount, int pendingMinutes, int pendingCount)
        {
            int duration = NormalizeDuration(ParseMinutes(task.EstimateText));
            string subject = NormalizeSubject(task.Subject);

            if (task.PriorityWeight >= 3)
            {
                return "ابدأ الآن بـ " + task.Title + " من " + subject + " لمدة " + duration + " دقيقة. مجرد جولة واحدة ستخفف الضغط فورًا.";
            }

            if (completedCount == 0)
            {
                return "لا تحمل هم اليوم كاملًا. افتح " + task.Title + " فقط، واضبط المؤقت " + duration + " دقيقة، وبعدها قرر الخطوة التالية.";
            }

            if (pendingCount == 1)
            {
                return "هذه تقريبًا آخر قفلة. أنجز " + task.Title + " وبعدها اعتبر اليوم ناجحًا رسميًا.";
            }

            if (pendingMinutes >= 150)
            {
                return "اليوم مزدحم، فخذه على دفعات قصيرة. ابدأ بـ " + task.Title + " ثم خذ استراحة صغيرة قبل المهمة التالية.";
            }

            return "مزاج الطالب يرتفع لما يبدأ بشيء واضح. ابدأ بـ " + task.Title + " من " + subject + " وخلك على جولة واحدة فقط.";
        }

        private static string BuildRewardIdea(StudyTask task, int completedCount)
        {
            int duration = NormalizeDuration(ParseMinutes(task.EstimateText));

            if (completedCount >= 3) return "المكافأة: قهوة + 10 دقائق راحة";
            if (task.PriorityWeight >= 3) return "بعدها: 5 دقائق تنفّس + مشروب";
            if (duration >= 45) return "بعدها: سنّاك سريع أو تمشية قصيرة";

            return "بعدها: أغنية تحبها أو دقيقتان تمدد";
        }

        private static string BuildCompanionText(StudyTask task)
        {
            string subject = NormalizeSubject(task.Subject);

            if (ContainsAny(subject, "رياض", "Math", "فيز", "كيمي")) return "العدة: ورقة أخطاء + قلم + جوال بعيد";
            if (ContainsAny(subject, "عرب", "إنج", "لغة", "تاريخ", "أحياء")) return "العدة: ماء + قراءة بصوت خفيف + مؤقت";

            return "العدة: مؤقت 25 دقيقة + ماء + سطح مكتب نظيف";
        }

        private static DateTime RoundToNextQuarter(DateTime dateTime)
        {
            DateTime baseTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);
            int nextQuarter = ((dateTime.Minute / 15) + 1) * 15;
            return nextQuarter >= 60 ? baseTime.AddHours(1) : baseTime.AddMinutes(nextQuarter);
        }

        private static string NormalizeSubject(string subject)
        {
            string value = (subject ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(value) ? "عام" : value;
        }

        private static int GetSubjectPaletteIndex(string subject)
        {
            int seed = NormalizeSubject(subject).Where(ch => !char.IsWhiteSpace(ch)).Sum(ch => ch);
            return seed % AccentPalette.Length;
        }

        private static Brush CreateBrush(string hexColor)
        {
            return (Brush)new BrushConverter().ConvertFromString(hexColor);
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            return tokens.Any(token => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void ResetQuickAddFields()
        {
            NewTaskTitle = "";
            SelectedSubjectOption = "";
            SelectedDurationOption = "30";
            SelectedPriorityOption = PriorityOptions[0];
        }

        private void ConfigureTaskSorting()
        {
            var taskView = CollectionViewSource.GetDefaultView(StudyTasks);
            taskView.SortDescriptions.Clear();
            taskView.SortDescriptions.Add(new SortDescription("IsDone", ListSortDirection.Ascending));
            taskView.SortDescriptions.Add(new SortDescription("PriorityWeight", ListSortDirection.Descending));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        protected bool SetProperty<T>(ref T f, T v, [CallerMemberName] string p = null)
        {
            if (EqualityComparer<T>.Default.Equals(f, v)) return false;
            f = v; OnPropertyChanged(p); return true;
        }
    }
}
