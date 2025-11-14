using System;

namespace PressureTimerApp
{
    public class TimerRecord
    {
        public string Barcode { get; set; }
        public string TimerCode1 { get; set; }
        public string TimerCode2 { get; set; }
        public int DurationSeconds { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime DatabaseTime { get; set; }
        public string InputMode { get; set; }

        public TimerRecord()
        {
            StartTime = DateTime.Now;
        }
    }
}