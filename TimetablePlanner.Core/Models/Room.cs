using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Room
    {
        public int Id { get; set ; }
        public string Name { get ; set ; }
        public int Capacity { get ; set ; }
        public List<Subject> Subjects { get ; set ; }

        public List<LessonRequirement> LessonRequirements { get; set; } = new();

        // Constructor for Room with all properties
        public Room(string name, int capacity, List<Subject> subjects)
        {
            this.Name = name;
            this.Capacity = capacity;
            this.Subjects = subjects;
        }

        // Parameterless constructor for Teacher
        public Room() { }


        //befejezetlen, minden tantárgy hozzáadása ami csak van
        public void AddAllSubjects() { }

        public void AddSubject(Subject subject)
        {
            this.Subjects.Add(subject);
        }

        public void RemoveSubject(Subject subject)
        {
            this.Subjects.Remove(subject);
        }

    }
}
