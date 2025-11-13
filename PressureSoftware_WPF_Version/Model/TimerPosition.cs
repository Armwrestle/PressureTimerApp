using System;

namespace PressureTimerApp
{
    public class TimerPosition
    {
        public char RowChar { get; set; }
        public int Number { get; set; }
        public bool IsOddRow { get; set; }
        public int DisplayRow { get; set; }
        public int Column { get; set; }
        public bool IsValid { get; set; }

        public override string ToString()
        {
            return $"{RowChar}-{Number:D2} ({(IsOddRow ? "奇数行" : "偶数行")}, 行{DisplayRow}, 列{Column})";
        }
    }
}