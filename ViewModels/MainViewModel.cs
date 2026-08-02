using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using SmartStudy.Core;
using SmartStudy.Models;
using SmartStudy.Services;

namespace SmartStudy.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly StorageService _storageService;
    private readonly RecommendationService _recommendationService;
    private readonly FileDialogService _fileDialogService;
    private readonly DispatcherTimer _focusTimer;
    private readonly DispatcherTimer _clockTimer;
    private AppState _state;

    private string _currentPage = "Dashboard";
    private string _searchText = string.Empty;
    private FilterChoice _selectedFilter;
    private string _newTaskTitle = string.Empty;
    private string _newTaskSubject = string.Empty;
    private string _newTaskNotes = string.Empty;
    private DateTime _newTaskDueDate = DateTime.Today.AddDays(1);
    private int _newTaskEstimatedMinutes = 45;
    private PriorityChoice _selectedPriority;
    private StudyTask? _selectedFocusTask;
    private StudyTask? _recommendedTask;
    private bool _isTimerRunning;
    private bool _isFocusMode = true;
    private int _phaseDurationSeconds;
    private int _remainingSeconds;
    private string _statusMessage = "جاهز لتنظيم يومك.";
    private string _currentTimeText = string.Empty;
    private string _currentDateText = string.Empty;

    public MainViewModel()
    {
        _storageService = new StorageService();
        _recommendationService = new RecommendationService();
        _fileDialogService = new FileDialogService();
        _state = _storageService.Load();

        PriorityOptions =
        [
            new PriorityChoice(StudyPriority.Low, "منخفضة"),
            new PriorityChoice(StudyPriority.Medium, "متوسطة"),
            new PriorityChoice(StudyPriority.High, "عالية"),
            new PriorityChoice(StudyPriority.Critical, "حرجة")
        ];
        _selectedPriority = PriorityOptions[1];

        FilterOptions =
        [
            new FilterChoice("All", "كل المهام"),
            new FilterChoice("Today", "اليوم"),
            new FilterChoice("Upcoming", "القادمة"),
            new FilterChoice("Overdue", "المتأخرة"),
            new FilterChoice("Completed", "المكتملة")
        ];
        _selectedFilter = FilterOptions[0];

        StudyTasks = new ObservableCollection<StudyTask>(_state.Tasks.OrderBy(task => task.IsCompleted).ThenBy(task => task.DueDate));
        StudySessions = new ObservableCollection<StudySession>(_state.Sessions.OrderByDescending(session => session.EndedAt));
        PendingTasks = [];
        DashboardTasks = [];
        RecentSessions = [];
        WeeklyStats = [];
        SubjectStats = [];

        foreach (var task in StudyTasks)
        {
            AttachTask(task);
        }

        TasksView = CollectionViewSource.GetDefaultView(StudyTasks);
        TasksView.Filter = FilterTask;
        TasksView.SortDescriptions.Add(new SortDescription(nameof(StudyTask.IsCompleted), ListSortDirection.Ascending));
        TasksView.SortDescriptions.Add(new SortDescription(nameof(StudyTask.DueDate), ListSortDirection.Ascending));

        NavigateCommand = new RelayCommand(parameter => Navigate(parameter?.ToString()));
        AddTaskCommand = new RelayCommand(AddTask, CanAddTask);
        ToggleTaskCommand = new RelayCommand(parameter => ToggleTask(parameter as StudyTask));
        DeleteTaskCommand = new RelayCommand(parameter => DeleteTask(parameter as StudyTask));
        FocusTaskCommand = new RelayCommand(parameter => FocusTask(parameter as StudyTask));
        ClearCompletedCommand = new RelayCommand(ClearCompleted, () => StudyTasks.Any(task => task.IsCompleted));
        StartPauseTimerCommand = new RelayCommand(StartPauseTimer);
        ResetTimerCommand = new RelayCommand(ResetTimer);
        SkipPhaseCommand = new RelayCommand(SkipPhase);
        ExportDataCommand = new RelayCommand(ExportData);
        ImportDataCommand = new RelayCommand(ImportData);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty, () => !string.IsNullOrWhiteSpace(SearchText));

        _focusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _focusTimer.Tick += OnFocusTimerTick;

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        ResetTimer();
        UpdateClock();
        RefreshDerivedData();
    }

    public ObservableCollection<StudyTask> StudyTasks { get; }
    public ObservableCollection<StudySession> StudySessions { get; }
    public ObservableCollection<StudyTask> PendingTasks { get; }
    public ObservableCollection<StudyTask> DashboardTasks { get; }
    public ObservableCollection<StudySession> RecentSessions { get; }
    public ObservableCollection<DailyFocusStat> WeeklyStats { get; }
    public ObservableCollection<SubjectStat> SubjectStats { get; }
    public ICollectionView TasksView { get; }
    public IReadOnlyList<PriorityChoice> PriorityOptions { get; }
    public IReadOnlyList<FilterChoice> FilterOptions { get; }

    public ICommand NavigateCommand { get; }
    public ICommand AddTaskCommand { get; }
    public ICommand ToggleTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    public ICommand FocusTaskCommand { get; }
    public ICommand ClearCompletedCommand { get; }
    public ICommand StartPauseTimerCommand { get; }
    public ICommand ResetTimerCommand { get; }
    public ICommand SkipPhaseCommand { get; }
    public ICommand ExportDataCommand { get; }
    public ICommand ImportDataCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public string CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(IsDashboardPage));
                OnPropertyChanged(nameof(IsTasksPage));
                OnPropertyChanged(nameof(IsFocusPage));
                OnPropertyChanged(nameof(IsAnalyticsPage));
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(PageSubtitle));
            }
        }
    }

    public bool IsDashboardPage => CurrentPage == "Dashboard";
    public bool IsTasksPage => CurrentPage == "Tasks";
    public bool IsFocusPage => CurrentPage == "Focus";
    public bool IsAnalyticsPage => CurrentPage == "Analytics";

    public string PageTitle => CurrentPage switch
    {
        "Tasks" => "إدارة المهام",
        "Focus" => "غرفة التركيز",
        "Analytics" => "تحليلات الدراسة",
        _ => "لوحة اليوم"
    };

    public string PageSubtitle => CurrentPage switch
    {
        "Tasks" => "حوّل المواد والواجبات إلى خطة واضحة قابلة للتنفيذ.",
        "Focus" => "جلسة واحدة، هدف واحد، وبدون تشتيت.",
        "Analytics" => "شاهد أين يذهب وقتك وكيف يتحسن أداؤك.",
        _ => "ابدأ بالمهمة الأهم، ثم دع الزخم يكمل الباقي."
    };

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? string.Empty))
            {
                TasksView.Refresh();
                OnPropertyChanged(nameof(HasFilteredTasks));
                (ClearSearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public FilterChoice SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (value is not null && SetProperty(ref _selectedFilter, value))
            {
                TasksView.Refresh();
                OnPropertyChanged(nameof(HasFilteredTasks));
            }
        }
    }

    public string NewTaskTitle
    {
        get => _newTaskTitle;
        set
        {
            if (SetProperty(ref _newTaskTitle, value ?? string.Empty))
            {
                (AddTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewTaskSubject
    {
        get => _newTaskSubject;
        set
        {
            if (SetProperty(ref _newTaskSubject, value ?? string.Empty))
            {
                (AddTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewTaskNotes
    {
        get => _newTaskNotes;
        set => SetProperty(ref _newTaskNotes, value ?? string.Empty);
    }

    public DateTime NewTaskDueDate
    {
        get => _newTaskDueDate;
        set => SetProperty(ref _newTaskDueDate, value);
    }

    public int NewTaskEstimatedMinutes
    {
        get => _newTaskEstimatedMinutes;
        set => SetProperty(ref _newTaskEstimatedMinutes, Math.Clamp(value, 5, 600));
    }

    public PriorityChoice SelectedPriority
    {
        get => _selectedPriority;
        set
        {
            if (value is not null)
            {
                SetProperty(ref _selectedPriority, value);
            }
        }
    }

    public StudyTask? SelectedFocusTask
    {
        get => _selectedFocusTask;
        set
        {
            if (SetProperty(ref _selectedFocusTask, value))
            {
                OnPropertyChanged(nameof(FocusTaskTitle));
                OnPropertyChanged(nameof(FocusTaskSubject));
            }
        }
    }

    public string FocusTaskTitle => SelectedFocusTask?.Title ?? "اختر مهمة قبل بدء الجلسة";
    public string FocusTaskSubject => SelectedFocusTask is null ? "لا توجد مهمة محددة" : $"{SelectedFocusTask.Subject} • {SelectedFocusTask.DurationText}";

    public bool IsTimerRunning
    {
        get => _isTimerRunning;
        private set
        {
            if (SetProperty(ref _isTimerRunning, value))
            {
                OnPropertyChanged(nameof(TimerActionText));
            }
        }
    }

    public bool IsFocusMode
    {
        get => _isFocusMode;
        private set
        {
            if (SetProperty(ref _isFocusMode, value))
            {
                OnPropertyChanged(nameof(TimerModeLabel));
                OnPropertyChanged(nameof(TimerHint));
            }
        }
    }

    public int RemainingSeconds
    {
        get => _remainingSeconds;
        private set
        {
            if (SetProperty(ref _remainingSeconds, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(TimerText));
                OnPropertyChanged(nameof(TimerProgress));
            }
        }
    }

    public string TimerText => $"{RemainingSeconds / 60:00}:{RemainingSeconds % 60:00}";
    public string TimerActionText => IsTimerRunning ? "إيقاف مؤقت" : RemainingSeconds < _phaseDurationSeconds ? "متابعة الجلسة" : "ابدأ الآن";
    public string TimerModeLabel => IsFocusMode ? "جلسة تركيز" : "استراحة قصيرة";
    public string TimerHint => IsFocusMode ? "أغلق الإشعارات واعمل على هدف واحد فقط." : "ابتعد عن الشاشة، اشرب ماء، وخذ نفسًا عميقًا.";
    public double TimerProgress => _phaseDurationSeconds <= 0 ? 0 : Math.Clamp((_phaseDurationSeconds - RemainingSeconds) * 100.0 / _phaseDurationSeconds, 0, 100);

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string CurrentTimeText
    {
        get => _currentTimeText;
        private set => SetProperty(ref _currentTimeText, value);
    }

    public string CurrentDateText
    {
        get => _currentDateText;
        private set => SetProperty(ref _currentDateText, value);
    }

    public string GreetingText
    {
        get
        {
            var hour = DateTime.Now.Hour;
            return hour switch
            {
                < 5 => "لسه صاحي؟ خلينا ننجز بهدوء.",
                < 12 => "صباح الإنجاز يا زيد",
                < 17 => "مساء التركيز يا زيد",
                < 22 => "مساء الخير يا زيد",
                _ => "اختم يومك بمهمة واضحة"
            };
        }
    }

    public bool HasTasks => StudyTasks.Count > 0;
    public bool HasFilteredTasks => !TasksView.IsEmpty;
    public bool HasPendingTasks => PendingTasks.Count > 0;
    public bool HasSessions => StudySessions.Count > 0;
    public int TotalTasks => StudyTasks.Count;
    public int CompletedTasks => StudyTasks.Count(task => task.IsCompleted);
    public int PendingTasksCount => StudyTasks.Count(task => !task.IsCompleted);
    public int OverdueTasksCount => StudyTasks.Count(task => task.IsOverdue);
    public int CompletedToday => StudyTasks.Count(task => task.CompletedAt?.Date == DateTime.Today);
    public int TodayFocusMinutes => StudySessions.Where(session => session.EndedAt.Date == DateTime.Today && session.WasCompleted).Sum(session => session.DurationMinutes);
    public int WeekFocusMinutes => StudySessions.Where(session => session.EndedAt.Date >= DateTime.Today.AddDays(-6) && session.WasCompleted).Sum(session => session.DurationMinutes);
    public double CompletionRate => TotalTasks == 0 ? 0 : CompletedTasks * 100.0 / TotalTasks;
    public string CompletionRateText => $"{CompletionRate:0}%";
    public string TodayFocusText => TodayFocusMinutes >= 60 ? $"{TodayFocusMinutes / 60}س {TodayFocusMinutes % 60}د" : $"{TodayFocusMinutes}د";
    public string WeekFocusText => WeekFocusMinutes >= 60 ? $"{WeekFocusMinutes / 60}س {WeekFocusMinutes % 60}د" : $"{WeekFocusMinutes}د";
    public double DailyGoalProgress => _state.Settings.DailyGoalMinutes <= 0 ? 0 : Math.Min(100, TodayFocusMinutes * 100.0 / _state.Settings.DailyGoalMinutes);
    public string DailyGoalText => $"{TodayFocusMinutes} / {_state.Settings.DailyGoalMinutes} دقيقة";
    public int StudyStreak => CalculateStudyStreak();
    public string StudyStreakText => StudyStreak switch { 0 => "ابدأ السلسلة اليوم", 1 => "يوم واحد", _ => $"{StudyStreak} أيام" };
    public string TopSubjectText => SubjectStats.FirstOrDefault()?.Subject ?? "لم تبدأ جلسات بعد";

    public StudyTask? RecommendedTask
    {
        get => _recommendedTask;
        private set
        {
            if (SetProperty(ref _recommendedTask, value))
            {
                OnPropertyChanged(nameof(RecommendedTaskTitle));
                OnPropertyChanged(nameof(RecommendedTaskMeta));
                OnPropertyChanged(nameof(RecommendationReason));
                OnPropertyChanged(nameof(HasRecommendation));
            }
        }
    }

    public bool HasRecommendation => RecommendedTask is not null;
    public string RecommendedTaskTitle => RecommendedTask?.Title ?? "لا توجد مهام معلقة";
    public string RecommendedTaskMeta => RecommendedTask is null ? "خطتك نظيفة حاليًا" : $"{RecommendedTask.Subject} • {RecommendedTask.DurationText} • {RecommendedTask.DueText}";
    public string RecommendationReason => _recommendationService.Explain(RecommendedTask);

    public void Dispose()
    {
        _focusTimer.Stop();
        _clockTimer.Stop();
        PersistState();
    }

    private void Navigate(string? page)
    {
        if (page is "Dashboard" or "Tasks" or "Focus" or "Analytics")
        {
            CurrentPage = page;
        }
    }

    private bool CanAddTask()
        => !string.IsNullOrWhiteSpace(NewTaskTitle) && !string.IsNullOrWhiteSpace(NewTaskSubject);

    private void AddTask()
    {
        var task = new StudyTask
        {
            Title = NewTaskTitle,
            Subject = NewTaskSubject,
            Notes = NewTaskNotes,
            DueDate = NewTaskDueDate,
            EstimatedMinutes = NewTaskEstimatedMinutes,
            Priority = SelectedPriority.Value
        };

        AttachTask(task);
        StudyTasks.Add(task);
        NewTaskTitle = string.Empty;
        NewTaskSubject = string.Empty;
        NewTaskNotes = string.Empty;
        NewTaskEstimatedMinutes = 45;
        NewTaskDueDate = DateTime.Today.AddDays(1);
        SelectedPriority = PriorityOptions[1];
        StatusMessage = "تمت إضافة المهمة إلى خطتك.";
        PersistAndRefresh();
    }

    private void ToggleTask(StudyTask? task)
    {
        if (task is null)
        {
            return;
        }

        task.IsCompleted = !task.IsCompleted;
        StatusMessage = task.IsCompleted ? "ممتاز! تم تسجيل المهمة كمكتملة." : "تمت إعادة المهمة إلى قائمة العمل.";
        PersistAndRefresh();
    }

    private void DeleteTask(StudyTask? task)
    {
        if (task is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"هل تريد حذف «{task.Title}»؟",
            "حذف المهمة",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        DetachTask(task);
        StudyTasks.Remove(task);
        if (SelectedFocusTask?.Id == task.Id)
        {
            SelectedFocusTask = null;
        }

        StatusMessage = "تم حذف المهمة.";
        PersistAndRefresh();
    }

    private void FocusTask(StudyTask? task)
    {
        if (task is null || task.IsCompleted)
        {
            return;
        }

        SelectedFocusTask = task;
        CurrentPage = "Focus";
        ResetTimer();
        StatusMessage = $"جاهز لبدء جلسة على: {task.Title}";
    }

    private void ClearCompleted()
    {
        var completed = StudyTasks.Where(task => task.IsCompleted).ToList();
        if (completed.Count == 0)
        {
            return;
        }

        var result = MessageBox.Show(
            $"سيتم حذف {completed.Count} مهمة مكتملة. هل تريد المتابعة؟",
            "تنظيف المهام المكتملة",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (var task in completed)
        {
            DetachTask(task);
            StudyTasks.Remove(task);
        }

        StatusMessage = "تم تنظيف المهام المكتملة مع الإبقاء على سجل جلساتك.";
        PersistAndRefresh();
    }

    private void StartPauseTimer()
    {
        if (IsFocusMode && SelectedFocusTask is null)
        {
            SelectedFocusTask = RecommendedTask ?? PendingTasks.FirstOrDefault();
            if (SelectedFocusTask is null)
            {
                StatusMessage = "أضف مهمة أولًا قبل بدء جلسة التركيز.";
                return;
            }
        }

        if (IsTimerRunning)
        {
            _focusTimer.Stop();
            IsTimerRunning = false;
            StatusMessage = "تم إيقاف المؤقت مؤقتًا.";
        }
        else
        {
            _focusTimer.Start();
            IsTimerRunning = true;
            StatusMessage = IsFocusMode ? "جلسة التركيز تعمل الآن." : "وقت الاستراحة بدأ.";
        }
    }

    private void ResetTimer()
    {
        _focusTimer.Stop();
        IsTimerRunning = false;
        _phaseDurationSeconds = (IsFocusMode ? _state.Settings.FocusMinutes : _state.Settings.BreakMinutes) * 60;
        RemainingSeconds = _phaseDurationSeconds;
        OnPropertyChanged(nameof(TimerActionText));
        OnPropertyChanged(nameof(TimerProgress));
        StatusMessage = IsFocusMode ? "تم تجهيز جلسة تركيز جديدة." : "تم تجهيز الاستراحة.";
    }

    private void SkipPhase()
    {
        _focusTimer.Stop();
        IsTimerRunning = false;
        IsFocusMode = !IsFocusMode;
        ResetTimer();
        StatusMessage = IsFocusMode ? "تم الانتقال إلى جلسة تركيز جديدة." : "تم الانتقال إلى الاستراحة.";
    }

    private void OnFocusTimerTick(object? sender, EventArgs e)
    {
        if (RemainingSeconds > 0)
        {
            RemainingSeconds--;
            return;
        }

        _focusTimer.Stop();
        IsTimerRunning = false;

        if (IsFocusMode)
        {
            CompleteFocusSession();
            IsFocusMode = false;
            ResetTimer();
            StatusMessage = "أحسنت! تم حفظ الجلسة. خذ استراحة قصيرة.";

            if (_state.Settings.AutoStartBreak)
            {
                _focusTimer.Start();
                IsTimerRunning = true;
            }
        }
        else
        {
            IsFocusMode = true;
            ResetTimer();
            StatusMessage = "انتهت الاستراحة. اختر هدفك وابدأ جولة جديدة.";
        }
    }

    private void CompleteFocusSession()
    {
        var duration = Math.Max(1, _state.Settings.FocusMinutes);
        var session = new StudySession
        {
            TaskId = SelectedFocusTask?.Id,
            TaskTitle = SelectedFocusTask?.Title ?? "جلسة تركيز عامة",
            Subject = SelectedFocusTask?.Subject ?? "عام",
            StartedAt = DateTime.Now.AddMinutes(-duration),
            EndedAt = DateTime.Now,
            DurationMinutes = duration,
            WasCompleted = true
        };

        StudySessions.Insert(0, session);
        if (SelectedFocusTask is not null)
        {
            SelectedFocusTask.ActualFocusMinutes += duration;
        }

        PersistAndRefresh();
    }

    private bool FilterTask(object item)
    {
        if (item is not StudyTask task)
        {
            return false;
        }

        var matchesSearch = string.IsNullOrWhiteSpace(SearchText)
                            || task.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                            || task.Subject.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                            || task.Notes.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);

        if (!matchesSearch)
        {
            return false;
        }

        return SelectedFilter.Value switch
        {
            "Today" => task.IsDueToday,
            "Upcoming" => task.IsUpcoming,
            "Overdue" => task.IsOverdue,
            "Completed" => task.IsCompleted,
            _ => true
        };
    }

    private void AttachTask(StudyTask task) => task.PropertyChanged += OnTaskPropertyChanged;
    private void DetachTask(StudyTask task) => task.PropertyChanged -= OnTaskPropertyChanged;

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StudyTask.Title)
            or nameof(StudyTask.Subject)
            or nameof(StudyTask.DueDate)
            or nameof(StudyTask.EstimatedMinutes)
            or nameof(StudyTask.Priority)
            or nameof(StudyTask.IsCompleted)
            or nameof(StudyTask.ActualFocusMinutes))
        {
            PersistAndRefresh();
        }
    }

    private void PersistAndRefresh()
    {
        PersistState();
        RefreshDerivedData();
    }

    private void PersistState()
    {
        _state.Tasks = StudyTasks.ToList();
        _state.Sessions = StudySessions.ToList();

        try
        {
            _storageService.Save(_state);
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذر حفظ البيانات: " + ex.Message;
        }
    }

    private void RefreshDerivedData()
    {
        TasksView.Refresh();

        ReplaceCollection(
            PendingTasks,
            StudyTasks.Where(task => !task.IsCompleted)
                .OrderBy(task => task.IsOverdue ? 0 : task.IsDueToday ? 1 : 2)
                .ThenByDescending(task => task.Priority)
                .ThenBy(task => task.DueDate));

        ReplaceCollection(DashboardTasks, PendingTasks.Take(5));
        ReplaceCollection(RecentSessions, StudySessions.OrderByDescending(session => session.EndedAt).Take(6));

        RecommendedTask = _recommendationService.GetRecommendedTask(StudyTasks);
        if (SelectedFocusTask is null || SelectedFocusTask.IsCompleted || !StudyTasks.Contains(SelectedFocusTask))
        {
            SelectedFocusTask = RecommendedTask;
        }

        BuildWeeklyStats();
        BuildSubjectStats();

        OnPropertyChanged(nameof(HasTasks));
        OnPropertyChanged(nameof(HasFilteredTasks));
        OnPropertyChanged(nameof(HasPendingTasks));
        OnPropertyChanged(nameof(HasSessions));
        OnPropertyChanged(nameof(TotalTasks));
        OnPropertyChanged(nameof(CompletedTasks));
        OnPropertyChanged(nameof(PendingTasksCount));
        OnPropertyChanged(nameof(OverdueTasksCount));
        OnPropertyChanged(nameof(CompletedToday));
        OnPropertyChanged(nameof(TodayFocusMinutes));
        OnPropertyChanged(nameof(WeekFocusMinutes));
        OnPropertyChanged(nameof(CompletionRate));
        OnPropertyChanged(nameof(CompletionRateText));
        OnPropertyChanged(nameof(TodayFocusText));
        OnPropertyChanged(nameof(WeekFocusText));
        OnPropertyChanged(nameof(DailyGoalProgress));
        OnPropertyChanged(nameof(DailyGoalText));
        OnPropertyChanged(nameof(StudyStreak));
        OnPropertyChanged(nameof(StudyStreakText));
        OnPropertyChanged(nameof(TopSubjectText));
        (ClearCompletedCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void BuildWeeklyStats()
    {
        var raw = Enumerable.Range(0, 7)
            .Select(offset => DateTime.Today.AddDays(offset - 6))
            .Select(date => new
            {
                Date = date,
                Minutes = StudySessions
                    .Where(session => session.WasCompleted && session.EndedAt.Date == date)
                    .Sum(session => session.DurationMinutes)
            })
            .ToList();

        var max = Math.Max(30, raw.Max(item => item.Minutes));
        var culture = CultureInfo.GetCultureInfo("ar");
        var stats = raw.Select(item => new DailyFocusStat
        {
            Date = item.Date,
            DayLabel = culture.DateTimeFormat.GetAbbreviatedDayName(item.Date.DayOfWeek),
            Minutes = item.Minutes,
            BarHeight = item.Minutes == 0 ? 6 : Math.Max(14, item.Minutes * 118.0 / max)
        });

        ReplaceCollection(WeeklyStats, stats);
    }

    private void BuildSubjectStats()
    {
        var sessionGroups = StudySessions
            .Where(session => session.WasCompleted && !string.IsNullOrWhiteSpace(session.Subject))
            .GroupBy(session => session.Subject.Trim(), StringComparer.CurrentCultureIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Sum(session => session.DurationMinutes), StringComparer.CurrentCultureIgnoreCase);

        var completedGroups = StudyTasks
            .Where(task => task.IsCompleted && !string.IsNullOrWhiteSpace(task.Subject))
            .GroupBy(task => task.Subject.Trim(), StringComparer.CurrentCultureIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.CurrentCultureIgnoreCase);

        var subjects = sessionGroups.Keys.Union(completedGroups.Keys, StringComparer.CurrentCultureIgnoreCase).ToList();
        var totalMinutes = Math.Max(1, sessionGroups.Values.Sum());

        var stats = subjects
            .Select(subject => new SubjectStat
            {
                Subject = subject,
                Minutes = sessionGroups.GetValueOrDefault(subject),
                CompletedTasks = completedGroups.GetValueOrDefault(subject),
                Percentage = sessionGroups.GetValueOrDefault(subject) * 100.0 / totalMinutes,
                AccentHex = CreateAccentHex(subject)
            })
            .OrderByDescending(stat => stat.Minutes)
            .ThenByDescending(stat => stat.CompletedTasks)
            .Take(8);

        ReplaceCollection(SubjectStats, stats);
    }

    private int CalculateStudyStreak()
    {
        var activeDays = StudySessions
            .Where(session => session.WasCompleted)
            .Select(session => session.EndedAt.Date)
            .Distinct()
            .ToHashSet();

        if (activeDays.Count == 0)
        {
            return 0;
        }

        var cursor = activeDays.Contains(DateTime.Today) ? DateTime.Today : DateTime.Today.AddDays(-1);
        var streak = 0;
        while (activeDays.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    private void ExportData()
    {
        var path = _fileDialogService.ChooseExportPath();
        if (path is null)
        {
            return;
        }

        try
        {
            _state.Tasks = StudyTasks.ToList();
            _state.Sessions = StudySessions.ToList();
            _storageService.Export(_state, path);
            StatusMessage = "تم تصدير النسخة الاحتياطية بنجاح.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "فشل التصدير", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportData()
    {
        var path = _fileDialogService.ChooseImportPath();
        if (path is null)
        {
            return;
        }

        var result = MessageBox.Show(
            "سيتم استبدال بياناتك الحالية بالنسخة المختارة. هل تريد المتابعة؟",
            "استيراد نسخة احتياطية",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var imported = _storageService.Import(path);
            foreach (var task in StudyTasks)
            {
                DetachTask(task);
            }

            StudyTasks.Clear();
            StudySessions.Clear();

            _state = imported;
            foreach (var task in imported.Tasks)
            {
                AttachTask(task);
                StudyTasks.Add(task);
            }

            foreach (var session in imported.Sessions.OrderByDescending(session => session.EndedAt))
            {
                StudySessions.Add(session);
            }

            ResetTimer();
            PersistAndRefresh();
            StatusMessage = "تم استيراد النسخة الاحتياطية بنجاح.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "فشل الاستيراد", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var culture = CultureInfo.GetCultureInfo("ar");
        CurrentTimeText = now.ToString("hh:mm tt", culture);
        CurrentDateText = now.ToString("dddd، d MMMM yyyy", culture);
        OnPropertyChanged(nameof(GreetingText));
    }

    private static string CreateAccentHex(string value)
    {
        string[] palette = ["#6C63FF", "#0EA5E9", "#14B8A6", "#F97316", "#EC4899", "#8B5CF6", "#22C55E"];
        var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(value ?? string.Empty) & 0x7FFFFFFF;
        return palette[hash % palette.Length];
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
