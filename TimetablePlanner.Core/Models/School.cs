using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class School : ISchool
    {
        public List<IRoom> Rooms { get; set ; }
        public List<IClassGroup> ClassGroups { get; set ; }
        public List<ISubject> Subjects { get ; set ; }
        public List<ITeacher> Teachers { get; set; }

        // Constructor for School with all properties
        public School(List<IRoom> rooms, List<IClassGroup> classGroups, List<ISubject> subjects, List<ITeacher> teachers)
        {
            this.Rooms = rooms;
            this.ClassGroups = classGroups;
            this.Subjects = subjects;
            this.Teachers = teachers;
        }

        // Parameterless constructor for School
        public School() { }

    }
}
