using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Constraints.Hard_Constraints
{
    public class RoomCapacity : IHardConstraint
    {
        public string Name => "Room capacity";

        public bool IsSatisfied(Schedule schedule, Lesson lesson)
        {
            if (lesson.AssignedRoom == null || lesson.ClassGroup == null)
                return true;

            return lesson.AssignedRoom.Capacity >= lesson.ClassGroup.Size;
        }
    }
}