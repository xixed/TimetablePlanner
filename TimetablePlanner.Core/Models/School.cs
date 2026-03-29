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
        public List<Room> Rooms { get; set ; }
        public List<IClassGroup> ClassGroups { get; set ; }
        public List<Subject> Subjects { get ; set ; }
        public List<Teacher> Teachers { get; set; }

        // Constructor for School with all properties
        public School(List<Room> rooms, List<IClassGroup> classGroups, List<Subject> subjects, List<Teacher> teachers)
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
