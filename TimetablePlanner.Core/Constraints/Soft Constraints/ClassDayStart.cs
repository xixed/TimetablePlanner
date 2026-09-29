using System;
using System.Linq;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class ClassDayStart : ISoftConstraint
    {
        public const int PenaltyPerPeriod = 10;

        public string Name => "Class day starts in first period";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null || candidate.ClassGroup == null)
                return 0;

            var otherPeriods = schedule.Lessons
                .Where(l =>
                    !ReferenceEquals(l, candidate) &&
                    l.ClassGroup != null &&
                    l.AssignedTimeSlot != null &&
                    l.ClassGroup.Id == candidate.ClassGroup.Id &&
                    l.AssignedTimeSlot.Day == candidate.AssignedTimeSlot.Day)
                .Select(l => l.AssignedTimeSlot!.Period)
                .ToList();

            var period = candidate.AssignedTimeSlot.Period;

            // Az elsõ óra a napon: a teljes eltolás a jelöltet terheli.
            if (otherPeriods.Count == 0)
                return (period - 1) * PenaltyPerPeriod;

            // Egyébként csak a nap kezdetének változása számít (lehet negatív is,
            // így a greedy résszámítások összege a végsõ értékkel egyezik).
            var oldStart = otherPeriods.Min();
            var newStart = Math.Min(oldStart, period);

            return (newStart - oldStart) * PenaltyPerPeriod;
        }

        public int GetTotalPenalty(Schedule schedule)
        {
            return schedule.Lessons
                .Where(l => l.ClassGroup != null && l.AssignedTimeSlot != null)
                .GroupBy(l => (l.ClassGroup.Id, l.AssignedTimeSlot!.Day))
                .Sum(g => (g.Min(l => l.AssignedTimeSlot!.Period) - 1) * PenaltyPerPeriod);
        }
    }
}