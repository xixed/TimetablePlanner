using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Interfaces
{
    public interface IGenerator
    {
        List<IHardConstraint> _hardConstraints { get; set; }
        List<ISoftConstraint> _softConstraints { get; set; }
        Schedule Generate(List<LessonRequirement> requirements);
    }
}
