using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public string Name { get ; set ; }
        
        public List<Subject> Subjects { get; set; }

        public TeacherType TeacherType { get ; set ; }
        public List<TimeSlot> UnavailableTimeSlots { get ; set ; }

        // Constructor for Teacher with all properties
        public Teacher(string name, TeacherType teacherType, List<TimeSlot> unavailableTimeSlots)
        {
            this.Name = name;
            
            this.TeacherType = teacherType;
            this.UnavailableTimeSlots = unavailableTimeSlots;
        }
        
        // Parameterless constructor for Teacher
        public Teacher() { }



    }
}
