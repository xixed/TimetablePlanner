using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class TimeSlot : ITimeSlot
    {
        public int Day { get; set; }
        public int Period { get; set; }

        // Constructor for TimeSlot with all properties
        public TimeSlot(int day, int period)
        {
            this.Day = day;
            this.Period = period;
        }

        // Parameterless constructor for TimeSlot
        public TimeSlot() { }
    }
}
