using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Hard_Constraints
{
    public class RoomConflict : IConstraint
    {
        public string Name => "Room conflict constraint";

        public bool IsSatisfied(Schedule schedule, ILesson lesson)
        {
            if (lesson.AssignedTimeSlot == null)
            {
                return true;
            }

            foreach (var existingLesson in schedule.Lessons)
            {
                if (existingLesson.Id == lesson.Id)
                {
                    continue;
                }

                if (existingLesson.AssignedTimeSlot == null)
                {
                    continue;
                }

                bool sameRoom = existingLesson.Room.Id == lesson.Room.Id;
                bool sameDay = existingLesson.AssignedTimeSlot.Day == lesson.AssignedTimeSlot.Day;
                bool samePeriod = existingLesson.AssignedTimeSlot.Period == lesson.AssignedTimeSlot.Period;

                if (sameRoom && sameDay && samePeriod)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
