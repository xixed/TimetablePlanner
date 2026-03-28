using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Generators
{
    public class GreedyGenerator
    {
        private readonly List<IConstraint> _constraints;

        public GreedyGenerator(List<IConstraint> constraints)
        {
            _constraints = constraints;
        }

        public GenerationResult Generate(List<LessonRequirement> requirements)
        {
            var result = new GenerationResult();
            int lessonId = 1;

            foreach (var requirement in requirements)
            {
                
                int placedCount = 0;

                foreach (var timeSlot in requirement.PossibleTimeSlots)
                {
                    if (placedCount >= requirement.WeeklyHours)
                    {
                        break;
                    }

                    foreach (var room in requirement.SuitableRooms)
                    {
                        var lesson = new Lesson
                        {
                            Id = lessonId,
                            Subject = requirement.Subject,
                            Teacher = requirement.Teacher,
                            ClassGroup = requirement.ClassGroup,
                            Requirement = requirement,
                            AssignedTimeSlot = timeSlot,
                            AssignedRoom = room
                        };

                        bool isValid = _constraints.All(c => c.IsSatisfied(result.Schedule, lesson));

                        if (!isValid)
                        {
                            continue;
                        }

                        result.Schedule.Lessons.Add(lesson);
                        lessonId++;
                        placedCount++;
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
