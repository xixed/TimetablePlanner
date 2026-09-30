using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class ClassGap : ISoftConstraint
    {
        public const int PenaltyPerGap = 15;

        public string Name => "Class gap minimization";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null || candidate.ClassGroup == null)
                return 0;

            var before = schedule.Lessons
                .Where(l =>
                    !ReferenceEquals(l, candidate) &&
                    l.ClassGroup != null &&
                    l.AssignedTimeSlot != null &&
                    l.ClassGroup.Id == candidate.ClassGroup.Id &&
                    l.AssignedTimeSlot.Day == candidate.AssignedTimeSlot.Day)
                .Select(l => l.AssignedTimeSlot!.Period)
                .ToList();

            var after = before.Append(candidate.AssignedTimeSlot.Period);

            return (GapHelper.CountInternalGaps(after) - GapHelper.CountInternalGaps(before)) * PenaltyPerGap;
        }

        public int GetTotalPenalty(Schedule schedule)
        {
            return schedule.Lessons
                .Where(l => l.ClassGroup != null && l.AssignedTimeSlot != null)
                .GroupBy(l => (l.ClassGroup.Id, l.AssignedTimeSlot!.Day))
                .Sum(g => GapHelper.CountInternalGaps(g.Select(l => l.AssignedTimeSlot!.Period)) * PenaltyPerGap);
        }
    }
}
