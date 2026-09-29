using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Interfaces.Constraint
{
    public interface ISoftConstraint
    {
        string Name { get; }
        int GetPenalty(Schedule schedule, Lesson canditate);

        /// <summary>
        /// Penalty of the whole schedule. Override it when GetPenalty would count the same problem more than once.
        /// </summary>
        int GetTotalPenalty(Schedule schedule)
        {
            var total = 0;
            foreach (var lesson in schedule.Lessons)
            {
                total += GetPenalty(schedule, lesson);
            }

            return total;
        }
    }
}
