using System.Collections.Generic;
using System.Linq;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class ClassDayBalance : ISoftConstraint
    {
        public const int DaysPerWeek = 5;
        public const int PenaltyPerLessonDifference = 3;

        public string Name => "Class day balance";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null || candidate.ClassGroup == null)
                return 0;

            var counts = schedule.Lessons
                .Where(l =>
                    !ReferenceEquals(l, candidate) &&
                    l.ClassGroup != null &&
                    l.AssignedTimeSlot != null &&
                    l.ClassGroup.Id == candidate.ClassGroup.Id)
                .GroupBy(l => l.AssignedTimeSlot!.Day)
                .ToDictionary(g => g.Key, g => g.Count());

            var before = Spread(counts);

            var day = candidate.AssignedTimeSlot.Day;
            counts[day] = counts.GetValueOrDefault(day) + 1;

            return Spread(counts) - before;
        }

        public int GetTotalPenalty(Schedule schedule)
        {
            return schedule.Lessons
                .Where(l => l.ClassGroup != null && l.AssignedTimeSlot != null)
                .GroupBy(l => l.ClassGroup.Id)
                .Sum(g => Spread(g
                    .GroupBy(l => l.AssignedTimeSlot!.Day)
                    .ToDictionary(d => d.Key, d => d.Count())));
        }

        private static int Spread(Dictionary<int, int> countsByDay)
        {
            var values = Enumerable.Range(1, DaysPerWeek)
                .Select(d => countsByDay.GetValueOrDefault(d))
                .ToList();

            return (values.Max() - values.Min()) * PenaltyPerLessonDifference;
        }
    }
}