using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Interfaces
{
    public interface IGenerator
    {
        List<IConstraint> _constraints { get; set; }
        Schedule Generate(List<LessonRequirement> requirements);
    }
}
