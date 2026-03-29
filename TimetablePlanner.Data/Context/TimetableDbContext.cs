using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Models;


namespace TimetablePlanner.Data.Context
{
    public class TimetableDbContext : DbContext
    {
        public TimetableDbContext(DbContextOptions<TimetableDbContext> options) : base(options)
        {
        }
        
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TeacherType> TeacherTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<LessonRequirement> LessonRequirements { get; set; }


    }
}
