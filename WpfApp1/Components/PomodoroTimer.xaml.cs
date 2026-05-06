using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WpfApp1.Components
{
    public partial class PomodoroTimer : UserControl
    {
        private DispatcherTimer _timer;
        private TimeSpan _currentTime;
        private bool _isRunning;

        public PomodoroTimer()
        {
            InitializeComponent();
            _currentTime = TimeSpan.FromMinutes(25);
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_currentTime.TotalSeconds > 0)
            {
                _currentTime = _currentTime.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimeDisplay();
            }
            else
            {
                _timer.Stop();
                _isRunning = false;
                StartButton.Content = "ابدأ";
                MessageBox.Show("انتهى وقت التركيز! خذ استراحة قصيرة.", "بطل!");
            }
        }

        private void UpdateTimeDisplay()
        {
            TimeDisplay.Text = string.Format("{0:D2}:{1:D2}", _currentTime.Minutes, _currentTime.Seconds);
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
            _timer.Stop();
            _isRunning = false;
            _currentTime = TimeSpan.FromMinutes(25);
            UpdateTimeDisplay();
            StartButton.Content = "ابدأ";
        }
    }
}
