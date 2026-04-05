using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Group : ClassGroup
    {

        public List<Class> Classes { get; set; } = new List<Class>();

        
        // Constructor for Group without subjects
        public Group(string name, int size)
        {
            this.Name = name;
            this.Size = size;
            this.Subjects = new List<Subject>();
        }
        // Parameterless constructor for Group
        public Group() { this.Subjects = new List<Subject>(); }

        // Constructor for Group from classes
        public Group(string name, List<Class> classes, int size, Subject subject)
        {
            this.Name = name;
            this.Classes = classes;
            this.Size = size;
            this.Subjects.Add(subject);
        }

        
    }
}
