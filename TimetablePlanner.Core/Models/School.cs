using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class School : ISchool
    {
        public List<IRoom> Rooms { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<IClassGroup> ClassGroups { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<ISubject> Subjects { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<ITeacher> Teachers { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
