using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Lesson : ILesson
    {
        public int Id { get ; set; }
        public ISubject Subject { get; set ; }

        public ITeacher Teacher { get ; set ; }
        public IClassGroup ClassGroup { get ; set ; }
        public IRoom? AssignedRoom { get ; set ; }
        public ITimeSlot? AssignedTimeSlot { get ; set ; }
        public ILessonRequirement Requirement { get ; set; }

        // Constructor for Lesson with all properties
        public Lesson(ISubject subject, ITeacher teacher, IClassGroup classGroup)
        {
            this.Subject = subject;
            this.ClassGroup = classGroup;
            this.Teacher = teacher;
        }

        // Parameterless constructor for Teacher
        public Lesson() { }
    }
}
