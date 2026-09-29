using System;
using System.Collections.Generic;
using System.Linq;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class DoubleLessonPreference : ISoftConstraint
    {
        public const int Penalty = 8;

        public string Name => "Double lesson preference";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null || candidate.Requirement == null)
                return 0;

            var others = schedule.Lessons
                .Where(l =>
                    !ReferenceEquals(l, candidate) &&
                    l.Requirement != null &&
                    l.AssignedTimeSlot != null &&
                    l.Requirement.Id == candidate.Requirement.Id &&
                    l.AssignedTimeSlot.Day == candidate.AssignedTimeSlot.Day)
                .Select(l => l.AssignedTimeSlot!.Period)
                .ToList();

            var after = others.Append(candidate.AssignedTimeSlot.Period).ToList();

            // Marginális érték: a nap büntetésének változása a jelölt hozzáadásától.
            return DayPenalty(candidate.Requirement, after) - DayPenalty(candidate.Requirement, others);
        }

        public int GetTotalPenalty(Schedule schedule)
        {
            return schedule.Lessons
                .Where(l => l.Requirement != null && l.AssignedTimeSlot != null)
                .GroupBy(l => (RequirementId: l.Requirement.Id, l.AssignedTimeSlot!.Day))
                .Sum(g => DayPenalty(
                    g.First().Requirement,
                    g.Select(l => l.AssignedTimeSlot!.Period).ToList()));
        }

        /// <summary>
        /// Egy követelmény egy napi óráinak büntetése.
        /// Preferált dupla óránál: minden párosítatlan óra büntetett.
        /// Nem preferáltnál: minden szomszédos óra-pár büntetett.
        /// </summary>
        private static int DayPenalty(LessonRequirement requirement, List<int> periods)
        {
            if (periods.Count == 0)
                return 0;

            if (requirement.PreferDoubleLesson && requirement.WeeklyHours < 2)
                return 0; // egyetlen heti óránál nem lehet dupla

            var total = 0;

            foreach (var runLength in GetRunLengths(periods))
            {
                total += requirement.PreferDoubleLesson
                    ? (runLength % 2) * Penalty      // páratlan hosszúságnál marad egy magányos óra
                    : (runLength - 1) * Penalty;     // minden szomszédos pár büntetett
            }

            return total;
        }

        // Egymást követő periódusokból álló blokkok hossza.
        private static IEnumerable<int> GetRunLengths(List<int> periods)
        {
            var ordered = periods.Distinct().OrderBy(p => p).ToList();
            var length = 1;

            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i] - ordered[i - 1] == 1)
                {
                    length++;
                }
                else
                {
                    yield return length;
                    length = 1;
                }
            }

            yield return length;
        }
    }
}
