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
    public class GreedyGenerator : IGenerator
    {
        public List<IHardConstraint> _hardConstraints { get; set; }
        public List<ISoftConstraint> _softConstraints { get; set; } = new List<ISoftConstraint>();

        public GreedyGenerator(List<IHardConstraint> constraints)
        {
            _hardConstraints = constraints;
        }



        public Schedule Generate(List<LessonRequirement> requirements)
        {
            var result = new Schedule();
            int lessonId = 1;

            foreach (var requirement in requirements)
            {
                if (requirement == null)
                    continue;

                if (requirement.PossibleTimeSlots == null || requirement.SuitableRooms == null)
                {
                    result.UnfulfilledRequirements.Add(requirement);
                    continue;
                }

                int placedCount = 0;

                foreach (var timeSlot in requirement.PossibleTimeSlots)
                {
                    if (placedCount >= requirement.WeeklyHours)
                        break;

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

                        bool isValid = _hardConstraints.All(c => c.IsSatisfied(result, lesson));

                        if (!isValid)
                            continue;

                        result.AddLesson(lesson);
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
