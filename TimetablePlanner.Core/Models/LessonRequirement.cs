using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class LessonRequirement
    {
        public Subject Subject { get ; set; }
        public Teacher Teacher { get ; set ; }
        public ClassGroup ClassGroup { get ; set ; }
        public List<TimeSlot> PossibleTimeSlots { get; set ; }
        public int WeeklyHours { get; set; }
        public List<Room> SuitableRooms { get ; set; }
        public int Id { get; set; }

        public bool PreferDoubleLesson { get; set; } = false;
    }
}
