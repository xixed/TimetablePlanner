using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Room : IRoom
    {
        public int Id { get; set ; }
        public string Name { get ; set ; }
        public int Capacity { get ; set ; }
        public List<ISubject> Subjects { get ; set ; }

        // Constructor for Room with all properties
        public Room(string name, int capacity, List<ISubject> subjects)
        {
            this.Name = name;
            this.Capacity = capacity;
            this.Subjects = subjects;
        }

        //befejezetlen, minden tantárgy hozzáadása ami csak van
        public void AddAllSubjects() { }

        public void AddSubject(ISubject subject)
        {
            this.Subjects.Add(subject);
        }

        public void RemoveSubject(ISubject subject)
        {
            this.Subjects.Remove(subject);
        }

    }
}
