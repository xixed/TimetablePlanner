using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimetablePlanner.Core.Interfaces
{
    public interface ITimeSlot
    {
        int Day { get; set; }
        int Period { get; set; }
    }
}
