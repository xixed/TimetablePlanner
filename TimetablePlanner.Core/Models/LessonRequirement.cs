using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class LessonRequirement : ILessonRequirement
    {
        public ISubject Subject { get ; set; }
        public ITeacher Teacher { get ; set ; }
        public IClassGroup ClassGroup { get ; set ; }
        public List<ITimeSlot> PossibleTimeSlots { get; set ; }
        public int WeeklyHours { get; set; }
        public List<IRoom> SuitableRooms { get ; set; }
        public int Id { get; set; }
    }
}
