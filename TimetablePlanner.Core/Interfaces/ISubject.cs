using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimetablePlanner.Core.Interfaces
{
    public interface ISubject
    {
        int Id { get; set; }
        string Name { get; set; }
        int WeekylHours { get; set; }

    }
}
