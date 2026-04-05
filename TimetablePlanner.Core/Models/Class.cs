using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Class : ClassGroup
    {

        // Constructor for Class with all properties
        public Class(string name, int size, List<Subject> subjects) 
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
            this.Subjects = new List<Subject>();
        }

        // Parameterless constructor for Class
        public Class() { this.Subjects = new List<Subject>(); }


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
