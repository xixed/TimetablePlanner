using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Interfaces
{
    public interface IClassGroup
    {
        int Id { get; set; }
        string Name { get; set; }
        int Size { get; set; }
        List<Subject> Subjects { get; set; }

    }
}
