using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Teacher : ITeacher
    {
        public int Id { get; set; }
        public string Name { get ; set ; }
        public List<ISubject> Subjects { get ; set ; }
        public ITeacherType TeacherType { get ; set ; }
        public List<ITimeSlot> UnavailableTimeSlots { get ; set ; }

        // Constructor for Teacher with all properties
        public Teacher(string name, List<ISubject> subjects, ITeacherType teacherType, List<ITimeSlot> unavailableTimeSlots)
        {
            this.Name = name;
            this.Subjects = subjects;
            this.TeacherType = teacherType;
            this.UnavailableTimeSlots = unavailableTimeSlots;
        }
        
        // Parameterless constructor for Teacher
        public Teacher() { }



    }
}
