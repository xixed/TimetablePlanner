using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimetablePlanner.Core.Interfaces
{
    public interface ITeacher
    {
        int Id { get; set; }
        string Name { get; set; }
        List<ISubject> Subjects { get; set; }
        ITeacherType TeacherType { get; set; }
        List<ITimeSlot> UnavailableTimeSlots { get; set; }
    }
}
