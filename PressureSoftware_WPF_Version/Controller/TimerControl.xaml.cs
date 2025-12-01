using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PressureTimerApp
{
    public partial class TimerControl : UserControl, INotifyPropertyChanged
    {
        private CountdownTimer _timer;
        private string _barcode;
        private string _timerCode;
        private string _displayText;
        private Brush _statusColor = Brushes.LightGray;

        public CountdownTimer Timer => _timer;

        public string Barcode
        {
            get => _barcode;
            set
            {
                _barcode = value;
                OnPropertyChanged(nameof(Barcode));
            }
        }

        public string TimerCode
        {
            get => _timerCode;
            set
            {
                _timerCode = value;
                UpdateDisplayText();
                UpdateStatusColor();
            }
        }

        public string DisplayText
        {
            get => _displayText;
            private set
            {
                _displayText = value;
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public Brush StatusColor
        {
            get => _statusColor;
            private set
            {
                _statusColor = value;
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TimerControl()
        {
            InitializeComponent();
            DataContext = this;

            // 初始化默认值
            DisplayText = "";
        }

        public void Initialize(string timerCode, int durationSeconds, string barcode = "")
        {
            TimerCode = timerCode;
            Barcode = barcode;

            _timer = new CountdownTimer(0, timerCode, timerCode, durationSeconds);
            _timer.TimerTick += OnTimerTick;
            _timer.TimerCompleted += OnTimerCompleted;
            _timer.PropertyChanged += OnTimerPropertyChanged;

            UpdateDisplayText();
            UpdateStatusColor();
        }

        public void Start()
        {
            _timer?.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
        }

        private void OnTimerTick(CountdownTimer timer, int remainingSeconds)
        {
            UpdateDisplayText();
            UpdateStatusColor();
        }

        private void OnTimerCompleted(CountdownTimer timer)
        {
            UpdateDisplayText();
            UpdateStatusColor();

            // 播放完成提示音
            try
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"播放提示音失败: {ex.Message}");
            }
        }

        private void OnTimerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateDisplayText();
            UpdateStatusColor();
        }

        private void UpdateDisplayText()
        {
            if (_timer == null)
            {
                DisplayText = TimerCode;
                return;
            }

            if (_timer.IsRunning)
            {
                // 运行中：显示剩余时间
                DisplayText = _timer.RemainingTimeFormatted;
            }
            else if (_timer.IsCompleted)
            {
                // 已完成：显示"完成"和编码
                DisplayText = $"完成\n{TimerCode}";
            }
            else
            {
                // 未运行：显示编码
                DisplayText = TimerCode;
            }
        }

        private void UpdateStatusColor()
        {
            if (_timer == null)
            {
                StatusColor = Brushes.LightGray;
                return;
            }

            // 优先检查是否完成
            if (_timer.IsCompleted)
            {
                StatusColor = new SolidColorBrush(Color.FromRgb(255, 99, 71)); // 番茄红色
            }
            else if (_timer.IsRunning)
            {
                StatusColor = new SolidColorBrush(Color.FromRgb(144, 238, 144)); // 浅绿色
            }
            else if (_timer.IsPaused)
            {
                StatusColor = new SolidColorBrush(Color.FromRgb(255, 215, 0));   // 黄色
            }
            else
            {
                StatusColor = Brushes.LightGray; // 已停止
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.TimerTick -= OnTimerTick;
                _timer.TimerCompleted -= OnTimerCompleted;
                _timer.PropertyChanged -= OnTimerPropertyChanged;
                _timer.Dispose();
            }
        }
    }
}