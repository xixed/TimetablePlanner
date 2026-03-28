using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class TeacherType : ITeacherType
    {
        public int RequiredWeeklyHours { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int MaxWeeklyHours { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
