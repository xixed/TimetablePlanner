using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Hard_Constraints
{
    public class RoomConflict : IHardConstraint
    {
        public string Name => "Room conflict";

        public bool IsSatisfied(Schedule schedule, Lesson lesson)
        {
            if (lesson.AssignedTimeSlot == null || lesson.AssignedRoom == null)
                return true;

            foreach (var existing in schedule.Lessons)
            {
                if (existing.AssignedTimeSlot == null || existing.AssignedRoom == null)
                    continue;

                bool sameTime =
                    existing.AssignedTimeSlot.Day == lesson.AssignedTimeSlot.Day &&
                    existing.AssignedTimeSlot.Period == lesson.AssignedTimeSlot.Period;

                bool sameRoom =
                    existing.AssignedRoom.Id == lesson.AssignedRoom.Id;

                if (sameTime && sameRoom)
                    return false;
            }

            return true;

        }
    }
}
