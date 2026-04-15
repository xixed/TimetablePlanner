using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
using TimetablePlanner.Core.Constraints.Soft_Constraints;
using TimetablePlanner.Core.Generators;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;
using TimetablePlanner.Core.Services;
using TimetablePlanner.Data.Context;
using TimetablePlanner.Data.Repositories;
using TimetablePlanner.Data.Seed;


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


        //var path = "test_school.json";
        //var json = File.ReadAllText(path);

        //var root = JsonNode.Parse(json);

        //var lessonRequirements = root["lessonRequirements"].AsArray();

        //var rooms = Enumerable.Range(1, 25).ToArray();
        //var timeslots = Enumerable.Range(1, 50).ToArray();

        //foreach (var lr in lessonRequirements)
        //{
        //    lr["suitableRoomIds"] = JsonSerializer.SerializeToNode(rooms);
        //    lr["possibleTimeSlotIds"] = JsonSerializer.SerializeToNode(timeslots);
        //}

        //File.WriteAllText("test_school_fixed.json",
        //    root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        //Console.WriteLine("DONE");

        var options = new DbContextOptionsBuilder<TimetableDbContext>()
        .UseSqlite("Data Source=timetable.db")
        .Options;

        using var context = new TimetableDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Seed", "test_school.json");
        JsonImporter.ImportFromFile(context, jsonPath, clearExisting: true);

        MessageBox.Show("Import finished.", "Info");


        ScheduleGenerationServices scheduleService = new ScheduleGenerationServices(new SchoolRepository(context));

        var scheduleList = scheduleService.GenerateSchedule();



        var outputPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schedule_output.txt");

        using (var writer = new StreamWriter(outputPath))
        {

            foreach (var lesson in scheduleList[0].Lessons)
            {
                writer.WriteLine($"Lesson {lesson.Id}: {lesson.Subject.Name} with {lesson.Teacher.Name} for {lesson.ClassGroup.Name} at {lesson.AssignedTimeSlot.Day} {lesson.AssignedTimeSlot.Period} in {lesson.AssignedRoom.Name}");
            }

            foreach (var unfilled in scheduleList[0].UnScheduledLessons)
            {
                writer.WriteLine($"Unscheduled: {unfilled.Subject.Name} for {unfilled.ClassGroup.Name} ({unfilled.Requirement.WeeklyHours} hours/week)");
            }
            writer.WriteLine($"Total Penalty: {scheduleList[0].TotalPenalty}");
            writer.WriteLine("\n\n---\n\n");
        }

        MessageBox.Show($"Schedule generated and saved to {outputPath}", "Info");

    }


}
