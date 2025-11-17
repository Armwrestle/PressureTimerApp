using System;

namespace PressureTimerApp
{
    public class WorkstationSequence
    {
        public string CurrentWorkstation { get; set; }
        public string PreviousWorkstation { get; set; }
        public bool HasPreviousStation => !string.IsNullOrEmpty(PreviousWorkstation);

        public override string ToString()
        {
            return HasPreviousStation
                ? $"{PreviousWorkstation} → {CurrentWorkstation}"
                : $"{CurrentWorkstation} (初始站点)";
        }
    }
}