using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class UnscheduledLessonPenalty : ISoftConstraint
    {
        public const int PenaltyPerLesson = 10000;

        public string Name => "Unscheduled lesson penalty";

        public int GetPenalty(Schedule schedule, Lesson candidate) => 0;

        public int GetTotalPenalty(Schedule schedule)
        {
            return schedule.UnScheduledLessons.Count * PenaltyPerLesson;
        }
    }
}