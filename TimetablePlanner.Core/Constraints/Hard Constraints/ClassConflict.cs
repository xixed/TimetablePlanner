using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Hard_Constraints
{
    public class ClassConflict : IHardConstraint
    {
        public string Name => "Class group conflict";

        public bool IsSatisfied(Schedule schedule, Lesson lesson)
        {
            if (lesson.AssignedTimeSlot == null || lesson.ClassGroup == null)
            {
                return true;
            }

            foreach (var existingLesson in schedule.Lessons)
            {
                if (existingLesson.AssignedTimeSlot == null
                    || existingLesson.ClassGroup == null
                    || existingLesson.AssignedTimeSlot.Day != lesson.AssignedTimeSlot.Day
                    || existingLesson.AssignedTimeSlot.Period != lesson.AssignedTimeSlot.Period)
                {
                    continue;
                }

                if (Conflicts(existingLesson.ClassGroup, lesson.ClassGroup))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Conflicts(ClassGroup existing, ClassGroup candidate)
        {
            if (existing is Class existingClass && candidate is Class candidateClass)
            {
                return existingClass.Id == candidateClass.Id;
            }

            if (existing is Group existingGroup && candidate is Class candidateWholeClass)
            {
                return existingGroup.ContainsClass(candidateWholeClass);
            }

            if (existing is Class existingWholeClass && candidate is Group candidateGroup)
            {
                return candidateGroup.ContainsClass(existingWholeClass);
            }

            if (existing is Group existingParallelGroup && candidate is Group candidateParallelGroup)
            {
                var sameParallelKey = !string.IsNullOrWhiteSpace(existingParallelGroup.ParallelKey)
                    && string.Equals(existingParallelGroup.ParallelKey, candidateParallelGroup.ParallelKey, System.StringComparison.OrdinalIgnoreCase);

                return !sameParallelKey;
            }

            return false;
        }
    }
}
