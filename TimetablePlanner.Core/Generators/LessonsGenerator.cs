using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Generators
{
    public class LessonsGenerator
    {
        public List<Lesson> Generate(List<LessonRequirement> requirements)
        {
            var lessons = new List<Lesson>();
            int lessonId = 1;
            foreach (var requirement in requirements)
            {
                for (int i = 0; i < requirement.WeeklyHours; i++)
                {
                    var lesson = new Lesson
                    {
                        Id = lessonId++,
                        Subject = requirement.Subject,
                        Teacher = requirement.Teacher,
                        ClassGroup = requirement.ClassGroup,
                        Requirement = requirement
                    };
                    lessons.Add(lesson);
                }
            }
            return lessons;


        }





    }
}
