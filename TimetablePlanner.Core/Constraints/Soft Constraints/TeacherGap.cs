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
        public string Name => "Teacher gap minimization";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null || candidate.Teacher == null)
                return 0;

            var teacherLessons = schedule.Lessons
                .Where(l =>
                    l.Teacher != null &&
                    l.AssignedTimeSlot != null &&
                    l.Teacher.Id == candidate.Teacher.Id)
                .ToList();

            var sameDayPeriods = teacherLessons
                .Where(l => l.AssignedTimeSlot!.Day == candidate.AssignedTimeSlot.Day)
                .Select(l => l.AssignedTimeSlot!.Period)
                .Append(candidate.AssignedTimeSlot.Period)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            if (sameDayPeriods.Count <= 2)
                return 0;

            int gaps = CountInternalGaps(sameDayPeriods);

            
            return gaps * 10;
        }

        private static int CountInternalGaps(List<int> orderedPeriods)
        {
            if (orderedPeriods.Count <= 1)
                return 0;

            int gaps = 0;

            for (int i = 1; i < orderedPeriods.Count; i++)
            {
                int diff = orderedPeriods[i] - orderedPeriods[i - 1];
                if (diff > 1)
                {
                    gaps += diff - 1;
                }
            }

            return gaps;
        }

    }
}
