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
        public int Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISubject Subject { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IClassGroup ClassGroup { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IRoom Room { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ITimeSlot AddignedTimeSlot { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<ITimeSlot> PossibleTimeSlots { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
