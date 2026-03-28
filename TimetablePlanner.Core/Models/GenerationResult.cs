using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimetablePlanner.Core.Models
{
    public class GenerationResult
    {
        public Schedule Schedule { get; set; } = new();
        public List<LessonRequirement> UnfulfilledRequirements { get; set; } = new();

    }
}
