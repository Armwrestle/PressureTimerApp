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
            get
            {
                if (_timer == null) return Brushes.LightGray;

                // 使用传统的 switch 语句替代 C# 8.0 的 switch 表达式
                switch (_timer.Status)
                {
                    case "运行中":
                        return new SolidColorBrush(Color.FromRgb(144, 238, 144)); // 浅绿色
                    case "已暂停":
                        return new SolidColorBrush(Color.FromRgb(255, 215, 0));   // 黄色
                    case "已完成":
                        return new SolidColorBrush(Color.FromRgb(50, 205, 50));   // 绿色
                    case "已停止":
                        return Brushes.LightGray;
                    default:
                        return Brushes.LightGray;
                }
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
        }

        private void OnTimerCompleted(CountdownTimer timer)
        {
            UpdateDisplayText();
        }

        private void OnTimerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateDisplayText();
            OnPropertyChanged(nameof(StatusColor));
        }

        private void UpdateDisplayText()
        {
            if (_timer != null && _timer.IsRunning)
            {
                // 运行中：显示剩余时间
                DisplayText = _timer.RemainingTimeFormatted;
            }
            else
            {
                // 未运行：显示编码
                DisplayText = TimerCode;
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