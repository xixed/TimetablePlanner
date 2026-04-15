using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            
            var lessonsToPlace = _lessonsGenerator.Generate(requirements);

            
            lessonsToPlace = lessonsToPlace
                .OrderBy(l => l.Requirement.PossibleTimeSlots.Count)
                .ThenBy(l => l.Requirement.SuitableRooms.Count)
                .ToList();

            foreach (var lesson in lessonsToPlace)
            {
                Lesson? bestCandidate = null;
                int bestPenalty = int.MaxValue;

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

                        bool hardOk = _hardConstraints.All(c => c.IsSatisfied(schedule, candidate));
                        if (!hardOk)
                            continue;

                        int penalty = _softConstraints.Sum(c => c.GetPenalty(schedule, candidate));

                        if (penalty < bestPenalty)
                        {
                            bestPenalty = penalty;
                            bestCandidate = candidate;
                        }
                    }
                }

                if (bestCandidate != null)
                {
                    schedule.AddLesson(bestCandidate);
                    schedule.TotalPenalty += bestPenalty;
                }
                else
                {
                    schedule.UnScheduledLessons.Add(lesson);
                }
            }

            return schedule;
        }



    }

    
}
