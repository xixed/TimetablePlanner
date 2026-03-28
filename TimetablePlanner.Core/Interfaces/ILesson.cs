using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimetablePlanner.Core.Interfaces
{
    public interface ILesson
    {
        int Id { get; set; }
        ISubject Subject { get; set; }
        IClassGroup ClassGroup { get; set; }
        IRoom Room { get; set; }
        ITimeSlot AddignedTimeSlot { get; set; }
        List<ITimeSlot> PossibleTimeSlots { get; set; }

    }
}
