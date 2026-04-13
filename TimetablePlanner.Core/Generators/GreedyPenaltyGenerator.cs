using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Generators
{
    public class GreedyPenaltyGenerator : IGenerator
    {
        
        public List<ISoftConstraint> _softConstraints { get; set; }
        public List<IHardConstraint> _hardConstraints { get; set; }

        public GreedyPenaltyGenerator(
            List<IHardConstraint> hardConstraints,
            List<ISoftConstraint>? softConstraints = null)
        {
            _hardConstraints = hardConstraints;
            _softConstraints = softConstraints ?? new List<ISoftConstraint>();
        }

        public Schedule Generate(List<LessonRequirement> requirements)
        {
            var result = new Schedule();
            int lessonId = 1;

            foreach (var requirement in requirements)
            {
                int placedCount = 0;

                for (int i = 0; i < requirement.WeeklyHours; i++)
                {
                    Lesson? bestLesson = null;
                    int bestPenalty = int.MaxValue;

                    foreach (var timeSlot in requirement.PossibleTimeSlots)
                    {
                        foreach (var room in requirement.SuitableRooms)
                        {
                            var candidate = new Lesson
                            {
                                Id = lessonId,
                                Subject = requirement.Subject,
                                Teacher = requirement.Teacher,
                                ClassGroup = requirement.ClassGroup,
                                Requirement = requirement,
                                AssignedTimeSlot = timeSlot,
                                AssignedRoom = room
                            };

                            bool isValid = _hardConstraints.All(c => c.IsSatisfied(result, candidate));
                            if (!isValid)
                                continue;

                            int penalty = _softConstraints.Sum(c => c.GetPenalty(result, candidate));

                            if (penalty < bestPenalty)
                            {
                                bestPenalty = penalty;
                                bestLesson = candidate;
                                
                            }
                        }
                    }

                    if (bestLesson != null)
                    {
                        result.Lessons.Add(bestLesson);
                        result.TotalPenalty = bestPenalty;
                        lessonId++;
                        placedCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (placedCount < requirement.WeeklyHours)
                {
                    result.UnfulfilledRequirements.Add(requirement);
                    
                }

                
            }

            

            return result;
        }
    }
}
