using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfApp1.Components
{
    public partial class PomodoroTimer : UserControl
    {
        private readonly TimeSpan _workDuration = TimeSpan.FromMinutes(25);
        private readonly TimeSpan _breakDuration = TimeSpan.FromMinutes(5);
        private readonly DispatcherTimer _timer;
        private TimeSpan _currentTime;
        private TimeSpan _sessionDuration;
        private bool _isRunning;
        private bool _isBreakSession;

        public PomodoroTimer()
        {
            InitializeComponent();

            _sessionDuration = _workDuration;
            _currentTime = _sessionDuration;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            UpdateTimeDisplay();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_currentTime.TotalSeconds > 1)
            {
                _currentTime = _currentTime.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimeDisplay();
                return;
            }

            _currentTime = TimeSpan.Zero;
            UpdateTimeDisplay();
            CompleteCurrentSession();
        }

        private void CompleteCurrentSession()
        {
            _timer.Stop();
            _isRunning = false;
            System.Media.SystemSounds.Asterisk.Play();

            if (!_isBreakSession)
            {
                _isBreakSession = true;
                _sessionDuration = _breakDuration;
                _currentTime = _sessionDuration;
                StartButton.Content = "ابدأ الراحة";
                UpdateTimeDisplay();
                MessageBox.Show("انتهت جلسة التركيز. خذ استراحة قصيرة ثم ارجع أقوى.", "جلسة مكتملة");
                return;
            }

            ResetToWorkSession();
            MessageBox.Show("انتهت الاستراحة. جاهز لجولة تركيز جديدة.", "عودة للتركيز");
        }

        private void UpdateTimeDisplay()
        {
            TimeDisplay.Text = string.Format("{0:D2}:{1:D2}", _currentTime.Minutes, _currentTime.Seconds);
            StatusDisplay.Text = _isBreakSession ? "استراحة قصيرة" : "وقت العمل";
            UpdateProgressArc();
        }

        private void UpdateProgressArc()
        {
            if (ProgressArc == null || _sessionDuration.TotalSeconds <= 0)
            {
                return;
            }

            double progress = 1.0 - (_currentTime.TotalSeconds / _sessionDuration.TotalSeconds);
            if (progress <= 0)
            {
                ProgressArc.Data = null;
                return;
            }

            double angle = Math.Min(359.9, progress * 360.0);
            double radius = 86.0;
            Point center = new Point(90.0, 90.0);
            Point start = new Point(center.X, center.Y - radius);
            double radians = (angle - 90.0) * Math.PI / 180.0;
            Point end = new Point(
                center.X + radius * Math.Cos(radians),
                center.Y + radius * Math.Sin(radians));

            var figure = new PathFigure { StartPoint = start, IsClosed = false };
            figure.Segments.Add(new ArcSegment
            {
                Point = end,
                Size = new Size(radius, radius),
                IsLargeArc = angle > 180.0,
                SweepDirection = SweepDirection.Clockwise
            });

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            ProgressArc.Data = geometry;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                _timer.Stop();
                StartButton.Content = "استئناف";
            }
            else
            {
                _timer.Start();
                StartButton.Content = "إيقاف";
            }

            _isRunning = !_isRunning;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetToWorkSession();
        }

        private void ResetToWorkSession()
        {
            _timer.Stop();
            _isRunning = false;
            _isBreakSession = false;
            _sessionDuration = _workDuration;
            _currentTime = _sessionDuration;
            UpdateTimeDisplay();
            StartButton.Content = "ابدأ";
        }
    }
}
