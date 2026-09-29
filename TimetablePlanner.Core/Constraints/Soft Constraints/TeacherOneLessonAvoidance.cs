using System.Linq;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class TeacherOneLessonAvoidance : ISoftConstraint
    {
        private const int Penalty = 20;

        public string Name => "Teacher one lesson day avoidance";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null || candidate.Teacher == null)
                return 0;

            var hasOtherLessonThatDay = schedule.Lessons.Any(l =>
                !ReferenceEquals(l, candidate) &&
                l.Teacher != null &&
                l.AssignedTimeSlot != null &&
                l.Teacher.Id == candidate.Teacher.Id &&
                l.AssignedTimeSlot.Day == candidate.AssignedTimeSlot.Day);

            return hasOtherLessonThatDay ? 0 : Penalty;
        }

        public int GetTotalPenalty(Schedule schedule)
        {
            return schedule.Lessons
                .Where(l => l.Teacher != null && l.AssignedTimeSlot != null)
                .GroupBy(l => (l.Teacher.Id, l.AssignedTimeSlot!.Day))
                .Count(g => g.Count() == 1) * Penalty;
        }
    }
}
