using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;

namespace TimetablePlanner.Core.Models
{
    public class Schedule
    {
        public List<ILesson> Lessons { get; set; }

        public Schedule() { }

        public void AddLesson(ILesson lesson)
        {
            this.Lessons.Add(lesson);
        }

        public void RemoveLesson(ILesson lesson)
        {
            this.Lessons.Remove(lesson);
        }
    }
}
