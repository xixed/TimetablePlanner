using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Interfaces
{
    public interface IConstraint
    {
        string Name { get; }
        bool IsSatisfied(Schedule schedule, ILesson lesson);
    }
}
