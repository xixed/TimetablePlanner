using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimetablePlanner.Core.Interfaces
{
    public interface IClassGroup
    {
        int Id { get; set; }
        string Name { get; set; }
        int Size { get; set; }
        List<ISubject> Subjects { get; set; }

    }
}
