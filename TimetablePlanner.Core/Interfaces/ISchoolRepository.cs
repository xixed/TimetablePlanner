using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Interfaces
{
    public interface ISchoolRepository
    {
        List<Teacher> GetTeachers();

        List<TeacherType> GetTeacherTypes();
        List<Subject> GetSubjects();
        List<Room> GetRooms();
        List<Class> GetClasses();
        List<Group> GetGroups();
        List<TimeSlot> GetTimeSlots();
        List<LessonRequirement> GetLessonRequirements();

        void AddTeacher(Teacher teacher);

        void AddTeacherType(TeacherType teacherType);
        void AddSubject(Subject subject);
        void AddRoom(Room room);
        void AddClass(Class classGroup);
        void AddGroup(Group group);
        void AddTimeSlot(TimeSlot timeSlot);
        void AddLessonRequirement(LessonRequirement requirement);

        void SaveChanges();
    }
}
