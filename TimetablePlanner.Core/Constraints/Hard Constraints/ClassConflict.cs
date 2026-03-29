using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Hard_Constraints
{
    public class ClassConflict : IConstraint
    {
        public string Name => "Class group conflict";

        public bool IsSatisfied(Schedule schedule, Lesson lesson)
        {
            if (lesson.AssignedTimeSlot == null)
            {
                return true;
            }

            foreach (var existingLesson in schedule.Lessons)
            {
                if (existingLesson.ClassGroup.Id != lesson.ClassGroup.Id)
                {
                    continue;
                }

                if (existingLesson.AssignedTimeSlot == null)
                {
                    continue;
                }

                bool sameGroup = existingLesson.ClassGroup.Id == lesson.ClassGroup.Id;
                if (!sameGroup)
                {
                    if (existingLesson.ClassGroup is Group existingGroup && lesson.ClassGroup is Class lessonClass)
                    {
                        sameGroup = existingGroup.Classes.Any(c => c.Id == lessonClass.Id);
                    }
                    else if (existingLesson.ClassGroup is Class existingClass && lesson.ClassGroup is Group lessonGroup)
                    {
                        sameGroup = lessonGroup.Classes.Any(c => c.Id == existingClass.Id);
                    }
                    else if (existingLesson.ClassGroup is Group eg && lesson.ClassGroup is Group lg)
                    {
                        // conflict if groups share any class, or same id already checked
                        sameGroup = eg.Classes.Select(c => c.Id).Intersect(lg.Classes.Select(c => c.Id)).Any();
                    }
                }

                bool sameDay = existingLesson.AssignedTimeSlot.Day == lesson.AssignedTimeSlot.Day;
                bool samePeriod = existingLesson.AssignedTimeSlot.Period == lesson.AssignedTimeSlot.Period;

                if (sameGroup && sameDay && samePeriod)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
