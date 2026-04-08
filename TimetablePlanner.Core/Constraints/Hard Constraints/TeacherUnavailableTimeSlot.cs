using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Hard_Constraints
{
    public class TeacherUnavailableTimeSlot : IHardConstraint
    {
        public string Name => "Teacher unavailable constraint";

        public bool IsSatisfied(Schedule schedule, Lesson lesson)
        {
            if (lesson.AssignedTimeSlot == null || lesson.Teacher?.UnavailableTimeSlots == null)
                return true;

            return !lesson.Teacher.UnavailableTimeSlots.Any(ts =>
                ts.Day == lesson.AssignedTimeSlot.Day &&
                ts.Period == lesson.AssignedTimeSlot.Period);

        }


    }
}
