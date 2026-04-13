using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class TeacherOneLessonAvoidance : ISoftConstraint
    {
        public string Name => "Teacher one lesson day avoidance";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null || candidate.Teacher == null)
                return 0;

            var sameDayLessons = schedule.Lessons
                .Where(l =>
                    l.Teacher != null &&
                    l.AssignedTimeSlot != null &&
                    l.Teacher.Id == candidate.Teacher.Id &&
                    l.AssignedTimeSlot.Day == candidate.AssignedTimeSlot.Day)
                .ToList();

            int dayLessonCountAfterPlacement = sameDayLessons.Count + 1;


            if (dayLessonCountAfterPlacement == 1)
                return 20;


            return 0;
        }
    }
}
