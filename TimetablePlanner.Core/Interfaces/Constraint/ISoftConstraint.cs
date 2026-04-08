using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Interfaces.Constraint
{
    public interface ISoftConstraint
    {
        string Name { get; }

        int GetPenalty(Schedule schedule, Lesson canditate);
    }
}
