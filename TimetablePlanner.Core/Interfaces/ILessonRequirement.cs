using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimetablePlanner.Core.Interfaces
{
    public interface ILessonRequirement
    {
        int Id { get; set; }
        ISubject Subject { get; set; }
        ITeacher Teacher { get; set; }
        IClassGroup ClassGroup { get; set; }
        List<ITimeSlot> PossibleTimeSlots { get; set; }
        int WeeklyHours { get; set; }
        List<IRoom> SuitableRooms { get; set; }
    }
}
