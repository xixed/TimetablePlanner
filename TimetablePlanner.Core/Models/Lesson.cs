using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Lesson
    {
        public int Id { get ; set; }
        public Subject Subject { get; set ; }

        public Teacher Teacher { get ; set ; }
        public ClassGroup ClassGroup { get ; set ; }
        public Room? AssignedRoom { get ; set ; }
        public TimeSlot? AssignedTimeSlot { get ; set ; }
        public LessonRequirement Requirement { get ; set; }

        // Parameterless constructor for Teacher
        public Lesson() { }
    }
}
