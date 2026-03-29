using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class TeacherType : ITeacherType
    {
        public int Id { get; set; }
        public int RequiredWeeklyHours { get; set; }
        public int MaxWeeklyHours { get; set; }

        // Constructor for TeacherType with all properties
        public TeacherType(int requiredWeeklyHours, int maxWeeklyHours)
        {
            this.RequiredWeeklyHours = requiredWeeklyHours;
            this.MaxWeeklyHours = maxWeeklyHours;
        }

        // Parameterless constructor for TeacherType
        public TeacherType() { }
    }
}
