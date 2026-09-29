using System;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class LateLesson : ISoftConstraint
    {
        public const int LastNormalPeriod = 8;
        public const int PenaltyPerPeriod = 5;

        public string Name => "Late lesson avoidance";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null)
                return 0;

            return Math.Max(0, candidate.AssignedTimeSlot.Period - LastNormalPeriod) * PenaltyPerPeriod;
        }
    }
}