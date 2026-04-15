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
        public List<Lesson> Lessons { get; set; } = new List<Lesson>();

        public List<Lesson> UnScheduledLessons { get; set; } = new();

        public int TotalPenalty { get; set; } = 0;

        public Schedule() { }

        public void AddLesson(Lesson lesson)
        {
            this.Lessons.Add(lesson);
        }

        public void RemoveLesson(Lesson lesson)
        {
            this.Lessons.Remove(lesson);
        }
    }
}
