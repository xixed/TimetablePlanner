using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TimetablePlanner.Core.Constraints.Hard_Constraints;
using TimetablePlanner.Core.Generators;
using TimetablePlanner.Core.Interfaces;
using TimetablePlanner.Core.Models;
using TimetablePlanner.Core.Services;
using TimetablePlanner.Data.Context;
using TimetablePlanner.Data.Repositories;


namespace TimetablePlanner.UI;


public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        RunTest();
    }

    private void RunTest()
    {
        // Subjects
        var math = new Subject
        {
            Id = 1,
            Name = "Matematika"
        };

        var english = new Subject
        {
            Id = 2,
            Name = "Angol"
        };

        var physics = new Subject
        {
            Id = 3,
            Name = "Fizika"
        };

        // Teacher type
        var teacherType = new TeacherType
        {
            RequiredWeeklyHours = 22,
            MaxWeeklyHours = 26
        };

        // Teachers
        var teacher1 = new Teacher
        {
            Id = 1,
            Name = "Kovács Anna",
            TeacherType = teacherType,
            Subjects = new List<Subject> { math, english }
        };

        var teacher2 = new Teacher
        {
            Id = 2,
            Name = "Nagy Béla",
            TeacherType = teacherType,
            Subjects = new List<Subject> { math, physics }
        };

        // Classes / groups
        var classA = new Class
        {
            Id = 1,
            Name = "9.A",
            Size = 28
        };

        var classB = new Class
        {
            Id = 2,
            Name = "9.B",
            Size = 30
        };

        // Rooms
        var room101 = new Room
        {
            Id = 1,
            Name = "101",
            Capacity = 30
        };

        var room102 = new Room
        {
            Id = 2,
            Name = "102",
            Capacity = 30
        };

        var lab = new Room
        {
            Id = 3,
            Name = "Labor",
            Capacity = 20
        };

        // Timeslots
        var timeSlots = new List<TimeSlot>
            {
                new TimeSlot { Id = 1, Day = 1, Period = 1 },
                new TimeSlot { Id = 2, Day = 1, Period = 2 },
                new TimeSlot { Id = 3, Day = 1, Period = 3 },
                new TimeSlot { Id = 4, Day = 1, Period = 4 },

                new TimeSlot { Id = 5, Day = 2, Period = 1 },
                new TimeSlot { Id = 6, Day = 2, Period = 2 },
                new TimeSlot { Id = 7, Day = 2, Period = 3 },
                new TimeSlot { Id = 8, Day = 2, Period = 4 },

                new TimeSlot { Id = 9, Day = 3, Period = 1 },
                new TimeSlot { Id = 10, Day = 3, Period = 2 },
                new TimeSlot { Id = 11, Day = 3, Period = 3 },
                new TimeSlot { Id = 12, Day = 3, Period = 4 }
            };

        // Requirements
        var requirements = new List<LessonRequirement>
            {
                new LessonRequirement
                {
                    Id = 1,
                    Subject = math,
                    Teacher = teacher1,
                    ClassGroup = classA,
                    WeeklyHours = 4,
                    SuitableRooms = new List<Room> { room101, room102 },
                    PossibleTimeSlots = new List<TimeSlot>{new TimeSlot { Id = 3, Day = 1, Period = 3 },
                new TimeSlot { Id = 4, Day = 1, Period = 4 },

                new TimeSlot { Id = 5, Day = 2, Period = 1 },
                new TimeSlot { Id = 6, Day = 2, Period = 2 },
                new TimeSlot { Id = 7, Day = 2, Period = 3 },
                new TimeSlot { Id = 8, Day = 2, Period = 4 },

                new TimeSlot { Id = 9, Day = 3, Period = 1 },
                new TimeSlot { Id = 10, Day = 3, Period = 2 },
                new TimeSlot { Id = 11, Day = 3, Period = 3 },
                new TimeSlot { Id = 12, Day = 3, Period = 4 } }
                },
                new LessonRequirement
                {
                    Id = 2,
                    Subject = english,
                    Teacher = teacher1,
                    ClassGroup = classA,
                    WeeklyHours = 2,
                    SuitableRooms = new List<Room> { room101, room102 },
                    PossibleTimeSlots = new List<TimeSlot>(timeSlots)
                },
                new LessonRequirement
                {
                    Id = 3,
                    Subject = physics,
                    Teacher = teacher2,
                    ClassGroup = classA,
                    WeeklyHours = 2,
                    SuitableRooms = new List<Room> { lab },
                    PossibleTimeSlots = new List<TimeSlot>(timeSlots)
                },
                new LessonRequirement
                {
                    Id = 4,
                    Subject = math,
                    Teacher = teacher2,
                    ClassGroup = classB,
                    WeeklyHours = 3,
                    SuitableRooms = new List<Room> { room101, room102 },
                    PossibleTimeSlots = new List<TimeSlot>(timeSlots)
                },
                new LessonRequirement
                {
                    Id = 5,
                    Subject = english,
                    Teacher = teacher1,
                    ClassGroup = classB,
                    WeeklyHours = 4,
                    SuitableRooms = new List<Room> { room102 },
                    PossibleTimeSlots = new List<TimeSlot>{new TimeSlot { Id = 1, Day = 1, Period = 1 },
                new TimeSlot { Id = 2, Day = 1, Period = 2 } }
                }
            };

        // Constraints
        var constraints = new List<IConstraint>
            {
                new TeacherConflict(),
                new ClassConflict(),
                new RoomConflict()
            };

        // Generator
        var generator = new GreedyGenerator(constraints);
        var result = generator.Generate(requirements);

        // Output
        var sb = new StringBuilder();
        sb.AppendLine("=== GENERÁLT ÓRAREND ===");
        sb.AppendLine();
        sb.AppendLine($"Generált órák száma: {result.Schedule.Lessons.Count}");
        sb.AppendLine($"Nem teljesült követelmények: {result.UnfulfilledRequirements.Count}");
        sb.AppendLine();

        foreach (var lesson in result.Schedule.Lessons
                     .OrderBy(l => l.AssignedTimeSlot?.Day)
                     .ThenBy(l => l.AssignedTimeSlot?.Period)
                     .ThenBy(l => l.AssignedRoom?.Name))
        {
            sb.AppendLine(
                $"{lesson.Subject.Name} | {lesson.ClassGroup.Name} | {lesson.Teacher.Name} | " +
                $"{lesson.AssignedTimeSlot?.Day}. nap / {lesson.AssignedTimeSlot?.Period}. óra | " +
                $"Terem: {lesson.AssignedRoom?.Name}");
        }

        if (result.UnfulfilledRequirements.Any())
        {
            sb.AppendLine();
            sb.AppendLine("=== NEM TELJESÜLT KÖVETELMÉNYEK ===");

            foreach (var req in result.UnfulfilledRequirements)
            {
                sb.AppendLine(
                    $"{req.Subject.Name} | {req.ClassGroup.Name} | " +
                    $"heti óraszám: {req.WeeklyHours}");
            }
        }

        MessageBox.Show(sb.ToString(), "Teszt eredmény");
    }


}
