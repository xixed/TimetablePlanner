using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces;
using TimetablePlanner.Core.Models;
using TimetablePlanner.Data.Context;

namespace TimetablePlanner.Data.Repositories
{
    public class SchoolRepository : ISchoolRepository
    {
        private readonly TimetableDbContext _context;
        public SchoolRepository(TimetableDbContext context)
        {
            _context = context;
        }

        public void AddClass(Class classGroup)
        {
            _context.Classes.Add(classGroup);
        }

        public void AddGroup(Group group)
        {
            _context.Groups.Add(group);
        }

        public void AddLessonRequirement(LessonRequirement requirement)
        {
            _context.LessonRequirements.Add(requirement);
        }

        public void AddRoom(Room room)
        {
            _context.Rooms.Add(room);
        }

        public void AddSubject(Subject subject)
        {
            _context.Subjects.Add(subject);
        }

        public void AddTeacher(Teacher teacher)
        {
            _context.Teachers.Add(teacher);
        }

        public void AddTeacherType(TeacherType teacherType)
        {
            _context.TeacherTypes.Add(teacherType);
        }

        public void AddTimeSlot(TimeSlot timeSlot)
        {
            _context.TimeSlots.Add(timeSlot);
        }

        public List<Class> GetClasses()
        {
            return _context.Classes.ToList();
        }

        public List<Group> GetGroups()
        {
            return _context.Groups.ToList();
        }

        public List<LessonRequirement> GetLessonRequirements()
        {
            return _context.LessonRequirements.ToList();
        }

        public List<Room> GetRooms()
        {
            return _context.Rooms.ToList();
        }

        public List<Subject> GetSubjects()
        {
            return _context.Subjects.ToList();
        }

        public List<Teacher> GetTeachers()
        {
            return _context.Teachers.ToList();
        }

        public List<TeacherType> GetTeacherTypes()
        {
            return _context.TeacherTypes.ToList();
        }

        public List<TimeSlot> GetTimeSlots()
        {
            return  _context.TimeSlots.ToList();
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}
