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
        public const int PenaltyPerBreak = 1;

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
                .Append(candidate.AssignedTimeSlot.Period);

            return CountBreaks(sameSubjectSameGroupSameDay) * PenaltyPerBreak;
        }

        public int GetTotalPenalty(Schedule schedule)
        {
            return schedule.Lessons
                .Where(l => l.ClassGroup != null && l.Subject != null && l.AssignedTimeSlot != null)
                .GroupBy(l => (ClassId: l.ClassGroup.Id, SubjectId: l.Subject.Id, l.AssignedTimeSlot!.Day))
                .Sum(g => CountBreaks(g.Select(l => l.AssignedTimeSlot!.Period)) * PenaltyPerBreak);
        }

        // Number of interruptions between the distinct periods (one per break, regardless of its length).
        private static int CountBreaks(IEnumerable<int> periods)
        {
            var ordered = periods.Distinct().OrderBy(p => p).ToList();
            var breaks = 0;

            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i] - ordered[i - 1] > 1)
                    breaks++;
            }

            return breaks;
        }
    }
}
