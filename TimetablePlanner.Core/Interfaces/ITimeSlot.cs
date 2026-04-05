using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Interfaces
{
    
    public interface ITimeSlot
    {

        public int Id { get; set; }
        int Day { get; set; }
        int Period { get; set; }
        public List<LessonRequirement> LessonRequirements { get; set; }
    }
}
