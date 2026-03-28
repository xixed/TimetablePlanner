using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Class : IClassGroup
    {
        public int Id { get; set; }
        public string Name { get ; set; }
        public int Size { get; set; }
        public List<ISubject> Subjects { get; set;} = new List<ISubject>();


        // Constructor for Class with all properties
        public Class(string name, int size, List<ISubject> subjects) 
        {
            this.Name = name;
            this.Size = size;
            this.Subjects = subjects;
        }

        // Constructor for Class without subjects
        public Class(string name, int size)
        {
            this.Name = name;
            this.Size = size;
        }

        // Parameterless constructor for Class
        public Class() { }


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
