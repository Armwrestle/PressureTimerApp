using System;
using System.Collections.Generic;
using System.Linq;

namespace PressureTimerApp
{
    public class TimerPositionMapper
    {
        private readonly Dictionary<string, TimerPosition> _positionMap = new Dictionary<string, TimerPosition>();
        private readonly int _columns;

        public TimerPositionMapper(int columns)
        {
            _columns = columns;
            InitializePositionMap();
        }

        private void InitializePositionMap()
        {
            // 为每个字母A-Z创建所有可能的计时器位置
            for (char row = 'A'; row <= 'Z'; row++)
            {
                // 每个字母有奇数和偶数行，总共 columns * 2 个计时器
                int totalTimers = _columns * 2;

                for (int i = 1; i <= totalTimers; i++)
                {
                    string code = $"{row}-{i:D2}";
                    _positionMap[code] = new TimerPosition
                    {
                        RowChar = row,
                        Number = i, // 直接使用数字作为列索引
                        IsOddRow = (i % 2) == 1,
                        DisplayRow = 0,
                        Column = i - 1, // 列索引从0开始
                        IsValid = true
                    };
                }
            }
        }

        public TimerPosition GetPosition(string timerCode)
        {
            if (_positionMap.TryGetValue(timerCode.ToUpper(), out var position))
            {
                return position;
            }
            return null;
        }

        public bool IsValidCode(string timerCode)
        {
            return _positionMap.ContainsKey(timerCode.ToUpper());
        }

        public IEnumerable<KeyValuePair<string, TimerPosition>> GetAllPositions()
        {
            return _positionMap;
        }
    }
}