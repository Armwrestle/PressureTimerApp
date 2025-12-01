using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace PressureTimerApp
{
    public class CountdownTimer : INotifyPropertyChanged
    {
        private readonly Timer _timer;
        private DateTime _startTime;
        private int _remainingSeconds;
        private int _totalSeconds;
        private bool _isRunning;
        private bool _isPaused;
        private bool _isCompleted;
        private DateTime _pauseTime;
        private readonly SynchronizationContext _syncContext;

        public int Id { get; }
        public string Name { get; set; }
        public string TimerCode { get; set; }
        public string Barcode { get; set; }

        public event Action<CountdownTimer> TimerCompleted;
        public event Action<CountdownTimer, int> TimerTick;
        public event PropertyChangedEventHandler PropertyChanged;

        public CountdownTimer(int id, string name, string timerCode, int totalSeconds)
        {
            Id = id;
            Name = name;
            TimerCode = timerCode;
            _totalSeconds = totalSeconds;
            _remainingSeconds = totalSeconds;

            // 捕获当前同步上下文（UI线程）
            _syncContext = SynchronizationContext.Current;
            if (_syncContext == null && Application.Current != null)
            {
                // 如果当前没有同步上下文，尝试使用Dispatcher的同步上下文
                _syncContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher);
            }

            _timer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void TimerCallback(object state)
        {
            if (_remainingSeconds <= 0)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _isRunning = false;
                _isCompleted = true;

                // 使用同步上下文通知UI线程
                NotifyUIThread(() =>
                {
                    TimerCompleted?.Invoke(this);
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(IsCompleted));
                });
                return;
            }

            _remainingSeconds--;

            // 使用同步上下文通知UI线程
            NotifyUIThread(() =>
            {
                TimerTick?.Invoke(this, _remainingSeconds);
                OnPropertyChanged(nameof(RemainingSeconds));
                OnPropertyChanged(nameof(RemainingTimeFormatted));
                OnPropertyChanged(nameof(Progress));
            });

            // 检查是否倒计时结束
            if (_remainingSeconds <= 0)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _isRunning = false;
                _isCompleted = true;

                NotifyUIThread(() =>
                {
                    TimerCompleted?.Invoke(this);
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(IsCompleted));
                });
            }
        }

        private void NotifyUIThread(Action action)
        {
            if (_syncContext != null)
            {
                _syncContext.Post(_ => action(), null);
            }
            else if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                // 如果没有UI上下文，直接执行（可能在测试环境中）
                action();
            }
        }

        public void Start()
        {
            if (_isRunning) return;

            if (_isPaused)
            {
                // 从暂停状态恢复
                var pauseDuration = DateTime.Now - _pauseTime;
                _startTime = _startTime.Add(pauseDuration);
                _isPaused = false;
            }
            else
            {
                // 全新开始
                _startTime = DateTime.Now;
                _remainingSeconds = _totalSeconds;
                _isCompleted = false; // 重置完成状态
            }

            _isRunning = true;

            // 使用同步上下文确保UI更新
            NotifyUIThread(() =>
            {
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsPaused));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(IsCompleted));
            });

            _timer.Change(0, 1000); // 每秒触发一次
        }

        public void Pause()
        {
            if (!_isRunning || _isPaused) return;

            _isRunning = false;
            _isPaused = true;
            _pauseTime = DateTime.Now;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            NotifyUIThread(() =>
            {
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsPaused));
                OnPropertyChanged(nameof(Status));
            });
        }

        public void Stop()
        {
            _isRunning = false;
            _isPaused = false;
            _isCompleted = false;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _remainingSeconds = _totalSeconds;

            NotifyUIThread(() =>
            {
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsPaused));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(RemainingSeconds));
                OnPropertyChanged(nameof(RemainingTimeFormatted));
                OnPropertyChanged(nameof(Progress));
            });
        }

        public void Reset(int newTotalSeconds)
        {
            Stop();
            _totalSeconds = newTotalSeconds;
            _remainingSeconds = newTotalSeconds;

            NotifyUIThread(() =>
            {
                OnPropertyChanged(nameof(RemainingSeconds));
                OnPropertyChanged(nameof(RemainingTimeFormatted));
                OnPropertyChanged(nameof(Progress));
            });
        }

        public int RemainingSeconds => _remainingSeconds;

        public string RemainingTimeFormatted
        {
            get
            {
                var timeSpan = TimeSpan.FromSeconds(_remainingSeconds);
                if (timeSpan.TotalHours >= 1)
                    return timeSpan.ToString(@"hh\:mm\:ss");
                else
                    return timeSpan.ToString(@"mm\:ss");
            }
        }

        public double Progress => _totalSeconds > 0
            ? 1.0 - ((double)_remainingSeconds / _totalSeconds)
            : 0;

        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;
        public bool IsCompleted => _isCompleted;
        public string Status => _isCompleted ? "已完成" : _isPaused ? "已暂停" : _isRunning ? "运行中" : "已停止";

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}