using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class DoubleLessonPreference : ISoftConstraint
    {
        public string Name => "Double lesson preference";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null || candidate.Requirement == null)
                return 0;

            if (!candidate.Requirement.PreferDoubleLesson)
                return 0;

            var sameRequirementLessons = schedule.Lessons
                .Where(l =>
                    l.Requirement?.Id == candidate.Requirement.Id &&
                    l.AssignedTimeSlot != null)
                .ToList();

            if (!sameRequirementLessons.Any())
            {
                
                return 2;
            }

            bool hasAdjacent = sameRequirementLessons.Any(l =>
                l.AssignedTimeSlot!.Day == candidate.AssignedTimeSlot.Day &&
                System.Math.Abs(l.AssignedTimeSlot.Period - candidate.AssignedTimeSlot.Period) == 1);

            return hasAdjacent ? 0 : 8;
        }
    }
}
