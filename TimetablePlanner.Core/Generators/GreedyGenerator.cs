using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Constraints.Soft_Constraints;
using TimetablePlanner.Core.Interfaces;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Generators
{
    public class GreedyGenerator
    {
        private readonly List<IHardConstraint> _hardConstraints;
        private readonly List<ISoftConstraint> _softConstraints;
        private readonly LessonsGenerator _lessonsGenerator;

        public GreedyGenerator(List<IHardConstraint> hardConstraints, List<ISoftConstraint> softConstraints, LessonsGenerator lessonsGenerator)
        {
            _hardConstraints = hardConstraints;
            _softConstraints = softConstraints;
            _lessonsGenerator = lessonsGenerator;
        }

        public Schedule Generate(List<LessonRequirement> requirements)
        {
            var schedule = new Schedule();

            var lessonsToPlace = _lessonsGenerator.Generate(requirements)
                .OrderBy(l => l.Requirement.PossibleTimeSlots.Count)
                .ThenBy(l => l.Requirement.SuitableRooms.Count)
                .ToList();

            foreach (var lesson in lessonsToPlace)
            {
                Lesson? bestCandidate = null;
                var bestScore = int.MaxValue;

                foreach (var timeSlot in lesson.Requirement.PossibleTimeSlots)
                {
                    foreach (var room in lesson.Requirement.SuitableRooms)
                    {
                        var candidate = new Lesson
                        {
                            Id = lesson.Id,
                            Subject = lesson.Subject,
                            Teacher = lesson.Teacher,
                            ClassGroup = lesson.ClassGroup,
                            Requirement = lesson.Requirement,
                            AssignedTimeSlot = timeSlot,
                            AssignedRoom = room
                        };

                        if (!_hardConstraints.All(c => c.IsSatisfied(schedule, candidate)))
                        {
                            continue;
                        }

                        schedule.AddLesson(candidate);
                        var score = ComputeTotalPenalty(schedule);
                        schedule.RemoveLesson(candidate);

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestCandidate = candidate;
                        }
                    }
                }

                var unscheduledScore = ComputeTotalPenaltyWithAdditionalUnscheduled(schedule, lesson);

                if (bestCandidate != null && bestScore <= unscheduledScore)
                {
                    schedule.AddLesson(bestCandidate);
                }
                else
                {
                    schedule.UnScheduledLessons.Add(lesson);
                }

                schedule.TotalPenalty = ComputeTotalPenalty(schedule);
            }

            return schedule;
        }

        private int ComputeTotalPenalty(Schedule schedule)
        {
            var total = 0;
            foreach (var sc in _softConstraints)
            {
                total += sc.GetTotalPenalty(schedule);
            }

            return total;
        }

        private static int ComputeTotalPenaltyWithAdditionalUnscheduled(Schedule schedule, Lesson lesson)
        {
            return schedule.TotalPenalty + UnscheduledLessonPenalty.PenaltyPerLesson;
        }
    }
}
