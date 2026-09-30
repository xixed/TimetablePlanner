using System.Linq;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Hard_Constraints
{
    public class ClassConflict : IHardConstraint
    {
        public string Name => "Class group conflict";

        public bool IsSatisfied(Schedule schedule, Lesson lesson)
        {
            if (lesson.AssignedTimeSlot == null)
            {
                return true;
            }

            foreach (var existingLesson in schedule.Lessons)
            {
                if (existingLesson.AssignedTimeSlot == null
                    || existingLesson.AssignedTimeSlot.Day != lesson.AssignedTimeSlot.Day
                    || existingLesson.AssignedTimeSlot.Period != lesson.AssignedTimeSlot.Period)
                {
                    continue;
                }

                if (Overlaps(existingLesson.ClassGroup, lesson.ClassGroup))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Overlaps(ClassGroup a, ClassGroup b)
        {
            if (a.Id == b.Id)
            {
                return true;
            }

            return (a, b) switch
            {
                (Group ga, Class cb) => ga.Classes.Any(c => c.Id == cb.Id),
                (Class ca, Group gb) => gb.Classes.Any(c => c.Id == ca.Id),
                (Group ga, Group gb) => ga.Classes.Select(c => c.Id).Intersect(gb.Classes.Select(c => c.Id)).Any(),
                _ => false
            };
        }
    }
}
