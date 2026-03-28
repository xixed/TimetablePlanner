using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Lesson : ILesson
    {
        public int Id { get ; set; }
        public ISubject Subject { get; set ; }
        public IClassGroup ClassGroup { get ; set ; }
        public IRoom Room { get ; set ; }
        public ITimeSlot? AssignedTimeSlot { get ; set ; }
        public List<ITimeSlot> PossibleTimeSlots { get ; set ; }


        // Constructor for Lesson with all properties
        public Lesson(ISubject subject, IClassGroup classGroup, IRoom room, List<ITimeSlot> possibleTimeSlots)
        {
            this.Subject = subject;
            this.ClassGroup = classGroup;
            this.Room = room;
            this.PossibleTimeSlots = possibleTimeSlots;
        }
    }
}
