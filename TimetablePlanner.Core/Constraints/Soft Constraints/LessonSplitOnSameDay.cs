using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class LessonSplitOnSameDay : ISoftConstraint
    {
        public string Name => "Lesson split on same day";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null)
                return 0;

            var sameSubjectSameGroupSameDay = schedule.Lessons
                .Where(l =>
                    l.ClassGroup.Id == candidate.ClassGroup.Id &&
                    l.Subject.Id == candidate.Subject.Id &&
                    l.AssignedTimeSlot != null &&
                    l.AssignedTimeSlot.Day == candidate.AssignedTimeSlot.Day)
                .Select(l => l.AssignedTimeSlot!.Period)
                .Append(candidate.AssignedTimeSlot.Period)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            if (sameSubjectSameGroupSameDay.Count <= 1)
                return 0;

            int gaps = 0;
            for (int i = 1; i < sameSubjectSameGroupSameDay.Count; i++)
            {
                if (sameSubjectSameGroupSameDay[i] - sameSubjectSameGroupSameDay[i - 1] > 1)
                    gaps++;
            }

            return gaps;
        }
    }
}
