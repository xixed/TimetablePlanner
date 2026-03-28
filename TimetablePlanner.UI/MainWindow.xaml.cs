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

namespace TimetablePlanner.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RunTest();
    }

    private void RunTest()
    {
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

        var teacherType = new TeacherType
        {
            RequiredWeeklyHours = 22,
            MaxWeeklyHours = 26
        };

        var teacher1 = new Teacher
        {
            Id = 1,
            Name = "Kovács Anna",
            TeacherType = teacherType,
            Subjects = new List<ISubject> { math, english }
        };

        var room1 = new Room
        {
            Id = 1,
            Name = "101",
            Capacity = 30,
            Subjects = new List<ISubject> { math, english }
        };

        var room2 = new Room
        {
            Id = 2,
            Name = "102",
            Capacity = 30,
            Subjects = new List<ISubject> { math, english }
        };

        var group1 = new Class
        {
            Id = 1,
            Name = "9.A",
            Size = 28,
            Subjects = new List<ISubject> { math, english }
        };

        var rooms = new List<IRoom> { room1, room2 };

        var timeSlots = new List<ITimeSlot>
        {
            new TimeSlot { Day = 1, Period = 1 },
            new TimeSlot { Day = 1, Period = 2 },
            new TimeSlot { Day = 1, Period = 3 },
            new TimeSlot { Day = 2, Period = 1 },
            new TimeSlot { Day = 2, Period = 2 }
        };

        // ✅ EZ AZ ÚJ RÉSZ
        var requirements = new List<LessonRequirement>
        {
            new LessonRequirement
            {
                Id = 1,
                Subject = math,
                Teacher = teacher1,
                ClassGroup = group1,
                WeeklyHours = 2,
                SuitableRooms = rooms,
                PossibleTimeSlots = timeSlots
            },
            new LessonRequirement
            {
                Id = 2,
                Subject = english,
                Teacher = teacher1,
                ClassGroup = group1,
                WeeklyHours = 2,
                SuitableRooms = rooms,
                PossibleTimeSlots = timeSlots
            }
        };

        var constraints = new List<IConstraint>
        {
            new TeacherConflict(),
            new ClassConflict(),
            new RoomConflict()
        };

        var generator = new GreedyGenerator(constraints);
        var result = generator.Generate(requirements);

        var sb = new StringBuilder();
        sb.AppendLine("Generált órarend:");
        sb.AppendLine();

        foreach (var lesson in result.Schedule.Lessons)
        {
            sb.AppendLine(
                $"{lesson.Subject.Name} - {lesson.ClassGroup.Name} - {lesson.Teacher.Name} - " +
                $"{lesson.AssignedTimeSlot?.Day}. nap / {lesson.AssignedTimeSlot?.Period}. óra - " +
                $"terem: {lesson.AssignedRoom?.Name}");
        }

        if (result.UnfulfilledRequirements.Any())
        {
            sb.AppendLine();
            sb.AppendLine("⚠ Nem teljesült követelmények:");

            foreach (var req in result.UnfulfilledRequirements)
            {
                sb.AppendLine($"{req.Subject.Name} - {req.ClassGroup.Name}");
            }
        }

        MessageBox.Show(sb.ToString(), "Teszt eredmény");
    }
}
