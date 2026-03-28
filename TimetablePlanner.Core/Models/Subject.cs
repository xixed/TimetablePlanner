using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Subject : ISubject
    {
        public int Id { get; set; }
        public string Name { get ; set ; }
        

        // Constructor for Subject with all properties
        public Subject(string name)
        {
            this.Name = name;
            
        }

        // Parameterless constructor for Subject
        public Subject() { }
    }
}
