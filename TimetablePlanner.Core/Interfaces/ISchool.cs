using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Interfaces
{
    public interface ISchool
    {
        List<Room> Rooms { get; set; }
        List<IClassGroup> ClassGroups { get; set; }
        List<Subject> Subjects { get; set; }
        List<Teacher> Teachers { get; set; }
    }
}
