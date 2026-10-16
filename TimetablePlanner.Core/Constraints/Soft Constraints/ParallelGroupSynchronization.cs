using System;
using System.Collections.Generic;
using System.Linq;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class ParallelGroupSynchronization : ISoftConstraint
    {
        public const int PenaltyPerMissingParallelLesson = 500;

        public string Name => "Parallel group synchronization";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null || candidate.ClassGroup is not Group group || candidate.Subject == null || string.IsNullOrWhiteSpace(group.ParallelKey))
            {
                return 0;
            }

            var sameBandAndSubjectLessons = schedule.Lessons
                .Where(l =>
                    !ReferenceEquals(l, candidate) &&
                    l.AssignedTimeSlot != null &&
                    l.Subject?.Id == candidate.Subject.Id &&
                    l.ClassGroup is Group otherGroup &&
                    !string.IsNullOrWhiteSpace(otherGroup.ParallelKey) &&
                    string.Equals(otherGroup.ParallelKey, group.ParallelKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (sameBandAndSubjectLessons.Count == 0)
            {
                return 0;
            }

            var sameTimeCount = sameBandAndSubjectLessons.Count(l =>
                l.AssignedTimeSlot!.Day == candidate.AssignedTimeSlot.Day &&
                l.AssignedTimeSlot.Period == candidate.AssignedTimeSlot.Period);

            return (sameBandAndSubjectLessons.Count - sameTimeCount) * PenaltyPerMissingParallelLesson;
        }

        public int GetTotalPenalty(Schedule schedule)
        {
            var groupedLessons = schedule.Lessons
                .Where(l =>
                    l.AssignedTimeSlot != null &&
                    l.Subject != null &&
                    l.ClassGroup is Group g &&
                    !string.IsNullOrWhiteSpace(g.ParallelKey))
                .GroupBy(l => new
                {
                    ParallelKey = ((Group)l.ClassGroup).ParallelKey!,
                    SubjectId = l.Subject.Id
                });

            var total = 0;
            foreach (var group in groupedLessons)
            {
                var slotGroups = group
                    .GroupBy(l => (l.AssignedTimeSlot!.Day, l.AssignedTimeSlot.Period))
                    .Select(g => g.Count())
                    .OrderByDescending(c => c)
                    .ToList();

                var expectedParallelCount = group
                    .Select(l => l.ClassGroup.Id)
                    .Distinct()
                    .Count();

                var lessonCount = group.Count();
                var expectedSlotCount = expectedParallelCount == 0 ? 0 : (int)Math.Ceiling((double)lessonCount / expectedParallelCount);

                for (var i = 0; i < expectedSlotCount; i++)
                {
                    var actualCount = i < slotGroups.Count ? slotGroups[i] : 0;
                    total += (expectedParallelCount - actualCount) * PenaltyPerMissingParallelLesson;
                }
            }

            return total;
        }
    }
}
