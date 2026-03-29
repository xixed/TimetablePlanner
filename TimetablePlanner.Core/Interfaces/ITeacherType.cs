using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimetablePlanner.Core.Interfaces
{
    public interface ITeacherType
    {
        public int Id { get; set; }
        int RequiredWeeklyHours { get; set; }
        int MaxWeeklyHours { get; set; }
    }
}
