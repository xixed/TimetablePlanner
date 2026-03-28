using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimetablePlanner.Core.Interfaces
{
    public interface ISchool
    {
        List<IRoom> Rooms { get; set; }
        List<IClassGroup> ClassGroups { get; set; }
        List<ISubject> Subjects { get; set; }
        List<ITeacher> Teachers { get; set; }
    }
}
