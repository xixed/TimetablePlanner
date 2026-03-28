using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Group : IClassGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Size { get; set; }
        public List<ISubject> Subjects { get; set ; } = new List<ISubject>();

        public List<Class> Classes { get; set; } = new List<Class>();

        
        // Constructor for Group without subjects
        public Group(string name, int size)
        {
            this.Name = name;
            this.Size = size;
        }
        // Parameterless constructor for Group
        public Group() { }

        // Constructor for Group from classes
        public Group(string name, List<Class> classes, int size, ISubject subject)
        {
            this.Name = name;
            this.Classes = classes;
            this.Size = size;
            this.Subjects.Add(subject);
        }

        
    }
}
