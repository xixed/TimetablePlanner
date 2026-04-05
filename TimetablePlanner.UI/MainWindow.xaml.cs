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
using TimetablePlanner.Data.Seed;
using System.IO;


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

        using var context = new TimetableDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Seed", "test_school.json");
        JsonImporter.ImportFromFile(context, jsonPath, clearExisting: true);

        MessageBox.Show("Import finished.", "Info");


        var constraints = new List<IConstraint>
        {
            new TeacherConflict(),
            new RoomConflict(),
            new ClassConflict(),
        };

        var generator = new GreedyGenerator(constraints);

        var requirements = context.LessonRequirements
            .Include(lr => lr.Subject)
            .Include(lr => lr.Teacher)
            .Include(lr => lr.ClassGroup)
            .Include(lr => lr.PossibleTimeSlots)
            .Include(lr => lr.SuitableRooms)
            .ToList();

        var schedule = generator.Generate(requirements);

        var outputPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schedule_output.txt");

        using (var writer = new StreamWriter(outputPath))
        {
            foreach (var lesson in schedule.Lessons)
            {
                writer.WriteLine($"Lesson {lesson.Id}: {lesson.Subject.Name} with {lesson.Teacher.Name} for {lesson.ClassGroup.Name} at {lesson.AssignedTimeSlot.Day} {lesson.AssignedTimeSlot.Period} in {lesson.AssignedRoom.Name}");
            }

            foreach (var unfilled in requirements.Where(r => !schedule.Lessons.Any(l => l.Requirement.Id == r.Id)))
            {
                writer.WriteLine($"Unscheduled: {unfilled.Subject.Name} for {unfilled.ClassGroup.Name} ({unfilled.WeeklyHours} hours/week)");
            }
        }

        MessageBox.Show($"Schedule generated and saved to {outputPath}", "Info");

    }


}
