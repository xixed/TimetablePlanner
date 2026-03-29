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
        var options = new DbContextOptionsBuilder<TimetableDbContext>()
            .UseSqlite("Data Source=timetable.db")
            .Options;

        var context = new TimetableDbContext(options);
        context.Database.EnsureCreated();

        var repository = new SchoolRepository(context);

        var service = new ScheduleGenerationServices(repository);

        var result = service.GenerateSchedule();

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
