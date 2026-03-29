using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Hard_Constraints
{
    public class TeacherConflict : IConstraint
    {
        public string Name => "Teacher conflict";

        public bool IsSatisfied(Schedule schedule, Lesson lesson)
        {
            if (lesson.AssignedTimeSlot == null)
            {
                return true;
            }

            foreach (var existingLesson in schedule.Lessons)
            {
                if (existingLesson.Teacher.Id != lesson.Teacher.Id)
                {
                    continue;
                }

                if (existingLesson.AssignedTimeSlot == null)
                {
                    continue;
                }

                bool sameTime = 
                    existingLesson.AssignedTimeSlot.Day == lesson.AssignedTimeSlot.Day &&
                    existingLesson.AssignedTimeSlot.Period == lesson.AssignedTimeSlot.Period;
                
                if (sameTime)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
