using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class TimeSlot : ITimeSlot
    {
        public int Day { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Period { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
