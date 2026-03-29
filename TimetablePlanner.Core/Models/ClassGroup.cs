using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class ClassGroup : IClassGroup
    {
        public int Id { get ; set ; }
        public string Name { get ; set ; }
        public int Size { get; set; }
        public List<Subject> Subjects { get; set; }

        // Constructor for ClassGroup with all properties
        public ClassGroup(string name, int size, List<Subject> subjects)
        {
            this.Name = name;
            this.Size = size;
            this.Subjects = subjects;
        }
        // Parameterless constructor for ClassGroup
        public ClassGroup() { }
    }
}
