using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Subject : ISubject
    {
        public int Id { get; set; }
        public string Name { get ; set ; }
        public int WeekylHours { get; set; }

        // Constructor for Subject with all properties
        public Subject(string name, int weeklyHours)
        {
            this.Name = name;
            this.WeekylHours = weeklyHours;
        }

        // Parameterless constructor for Subject
        public Subject() { }
    }
}
