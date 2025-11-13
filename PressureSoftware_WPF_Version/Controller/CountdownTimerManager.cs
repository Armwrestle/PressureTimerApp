using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PressureTimerApp
{
    public class CountdownTimerManager : IDisposable
    {
        private readonly ConcurrentDictionary<int, CountdownTimer> _timers;
        private int _nextId;

        public event Action<CountdownTimer> TimerAdded;
        public event Action<CountdownTimer> TimerRemoved;
        public event Action<CountdownTimer> TimerCompleted;

        public CountdownTimerManager()
        {
            _timers = new ConcurrentDictionary<int, CountdownTimer>();
            _nextId = 1;
        }

        public CountdownTimer CreateTimer(string name, string timerCode, int totalSeconds)
        {
            var timer = new CountdownTimer(_nextId++, name, timerCode, totalSeconds);

            timer.TimerCompleted += OnTimerCompleted;

            if (_timers.TryAdd(timer.Id, timer))
            {
                TimerAdded?.Invoke(timer);
                return timer;
            }

            return null;
        }

        public bool StartTimer(int id)
        {
            if (_timers.TryGetValue(id, out var timer))
            {
                timer.Start();
                return true;
            }
            return false;
        }

        public bool PauseTimer(int id)
        {
            if (_timers.TryGetValue(id, out var timer))
            {
                timer.Pause();
                return true;
            }
            return false;
        }

        public bool StopTimer(int id)
        {
            if (_timers.TryGetValue(id, out var timer))
            {
                timer.Stop();
                return true;
            }
            return false;
        }

        public bool RemoveTimer(int id)
        {
            if (_timers.TryRemove(id, out var timer))
            {
                timer.TimerCompleted -= OnTimerCompleted;
                TimerRemoved?.Invoke(timer);
                timer.Dispose();
                return true;
            }
            return false;
        }

        public CountdownTimer GetTimer(int id)
        {
            return _timers.TryGetValue(id, out var timer) ? timer : null;
        }

        public CountdownTimer GetTimerByCode(string timerCode)
        {
            return _timers.Values.FirstOrDefault(t => t.TimerCode == timerCode);
        }

        public IEnumerable<CountdownTimer> GetAllTimers()
        {
            return _timers.Values.ToList();
        }

        public IEnumerable<CountdownTimer> GetRunningTimers()
        {
            return _timers.Values.Where(t => t.IsRunning).ToList();
        }

        private void OnTimerCompleted(CountdownTimer timer)
        {
            TimerCompleted?.Invoke(timer);
        }

        public void Dispose()
        {
            foreach (var timer in _timers.Values)
            {
                timer.TimerCompleted -= OnTimerCompleted;
                timer.Dispose();
            }
            _timers.Clear();
        }
    }
}