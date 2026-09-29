using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class TeacherGap : ISoftConstraint
    {
        private const int PenaltyPerGap = 10;

        public string Name => "Teacher gap minimization";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null || candidate.Teacher == null)
                return 0;

            var before = schedule.Lessons
                .Where(l =>
                    !ReferenceEquals(l, candidate) &&
                    l.Teacher != null &&
                    l.AssignedTimeSlot != null &&
                    l.Teacher.Id == candidate.Teacher.Id &&
                    l.AssignedTimeSlot.Day == candidate.AssignedTimeSlot.Day)
                .Select(l => l.AssignedTimeSlot!.Period)
                .ToList();

            var after = before.Append(candidate.AssignedTimeSlot.Period);

            return (GapHelper.CountInternalGaps(after) - GapHelper.CountInternalGaps(before)) * PenaltyPerGap;
        }

        public int GetTotalPenalty(Schedule schedule)
        {
            return schedule.Lessons
                .Where(l => l.Teacher != null && l.AssignedTimeSlot != null)
                .GroupBy(l => (l.Teacher.Id, l.AssignedTimeSlot!.Day))
                .Sum(g => GapHelper.CountInternalGaps(g.Select(l => l.AssignedTimeSlot!.Period)) * PenaltyPerGap);
        }
    }
}
