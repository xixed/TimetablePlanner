using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    public class RoomStability : ISoftConstraint
    {
        public const int PenaltyPerExtraRoom = 5;

        public string Name => "Room stability";

        public int GetPenalty(Schedule schedule, Lesson candidate)
        {
            if (candidate.AssignedTimeSlot == null || candidate.AssignedRoom == null || candidate.ClassGroup == null)
                return 0;

            var sameDayLessons = schedule.Lessons
                .Where(l =>
                    l.ClassGroup != null &&
                    l.AssignedTimeSlot != null &&
                    l.AssignedRoom != null &&
                    l.ClassGroup.Id == candidate.ClassGroup.Id &&
                    l.AssignedTimeSlot.Day == candidate.AssignedTimeSlot.Day)
                .ToList();

            var distinctRoomIds = sameDayLessons
                .Select(l => l.AssignedRoom!.Id)
                .Append(candidate.AssignedRoom.Id)
                .Distinct()
                .ToList();

                
            int extraRooms = distinctRoomIds.Count - 1;

            return extraRooms > 0 ? extraRooms * PenaltyPerExtraRoom : 0;
        }

        public int GetTotalPenalty(Schedule schedule)
        {
            return schedule.Lessons
                .Where(l => l.ClassGroup != null && l.AssignedTimeSlot != null && l.AssignedRoom != null)
                .GroupBy(l => (l.ClassGroup.Id, l.AssignedTimeSlot!.Day))
                .Sum(g => (g.Select(l => l.AssignedRoom!.Id).Distinct().Count() - 1) * PenaltyPerExtraRoom);
        }
    }
}
