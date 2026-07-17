using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using TimetablePlanner.Core.Models;
using TimetablePlanner.Core.Services;
using TimetablePlanner.Data.Context;
using TimetablePlanner.Data.Repositories;
using TimetablePlanner.Data.Seed;

namespace TimetablePlanner.UI
{
    public partial class MainWindow : Window
    {
        private static readonly string[] HungarianDayShort =
            ["?", "H", "K", "Sze", "Cs", "P", "Szo", "Vas"];
        private readonly Dictionary<string, (int Day, int Period)> _slotByColumn = new();
        private readonly Dictionary<(string ClassName, int Day, int Period), List<Lesson>> _lessonsByCell = new();
        private bool _scheduleGridCompactView = true;

        public MainWindow()
        {
            InitializeComponent();
            StatusText.Text = "Készen áll. A Generálás gombbal készíthetsz órarendet.";
            ScheduleGrid.ItemsSource = null;
            ScheduleGrid.RowHeight = 86;
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.ContextMenu is not { } menu)
            {
                return;
            }

            menu.PlacementTarget = btn;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void BtnLessons_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Az órák szerkesztője később kerül ide.",
                "Órák",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnTeachers_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "A tanárok szerkesztője később kerül ide.",
                "Tanárok",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnRooms_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "A termek szerkesztője később kerül ide.",
                "Termek",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            _scheduleGridCompactView = !_scheduleGridCompactView;
            if (_scheduleGridCompactView)
            {
                ScheduleGrid.FontSize = 14;
                ScheduleGrid.RowHeight = 86;
                ScheduleGrid.ColumnHeaderHeight = 44;
                StatusText.Text = "Nézet: normál.";
            }
            else
            {
                ScheduleGrid.FontSize = 17;
                ScheduleGrid.RowHeight = 112;
                ScheduleGrid.ColumnHeaderHeight = 52;
                StatusText.Text = "Nézet: nagyított (távolabbról is olvasható).";
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schedule_output.txt");
            if (!File.Exists(outputPath))
            {
                MessageBox.Show("A kimeneti fájl még nem létezik. Előbb futtasd a generálást.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Megnyitás sikertelen", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Futtatás…";
                GenerationOverallProgressBar.Visibility = Visibility.Visible;
                GenerationOverallProgressBar.Value = 0;
                GenerationProgressBar.Visibility = Visibility.Visible;
                GenerationProgressBar.Value = 0;

                var options = new DbContextOptionsBuilder<TimetableDbContext>()
                    .UseSqlite("Data Source=timetable.db")
                    .Options;

                using var context = new TimetableDbContext(options);
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Seed", "test_school.json");
                JsonImporter.ImportFromFile(context, jsonPath, clearExisting: true);

                var scheduleService = new ScheduleGenerationServices(new SchoolRepository(context));

                var progress = new Progress<TimetablePlanner.Core.Models.GenerationProgressReport>(r =>
                {
                    var percent = Math.Max(0, Math.Min(100, r.StageProgress * 100));
                    GenerationProgressBar.Value = percent;
                    var overall = Math.Max(0, Math.Min(100, r.Overall * 100));
                    GenerationOverallProgressBar.Value = overall;
                    StatusText.Text = r.Stage != null ? $"{r.Stage}: {percent:0}%" : $"Futtatás… {percent:0}%";
                });

                var scheduleList = await System.Threading.Tasks.Task.Run(() => scheduleService.GenerateSchedule(progress));
                var schedule = scheduleList.Count > 0 ? scheduleList[0] : null;

                // mark generation as complete
                GenerationOverallProgressBar.Value = 100;
                RefreshScheduleGrid(schedule);

                var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schedule_output.txt");
                WriteScheduleTextFile(schedule, outputPath);

                StatusText.Text = schedule?.Lessons.Count > 0
                    ? $"Kész. {schedule.Lessons.Count} óra ütemezve. Kimenet: schedule_output.txt"
                    : "Kész. Nincs ütemezett óra.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Hiba.";
                MessageBox.Show(ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                GenerationProgressBar.Visibility = Visibility.Collapsed;
                GenerationProgressBar.Value = 0;
                GenerationOverallProgressBar.Visibility = Visibility.Collapsed;
                GenerationOverallProgressBar.Value = 0;
            }
        }

        private void BtnRecreateDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var options = new DbContextOptionsBuilder<TimetableDbContext>()
                    .UseSqlite("Data Source=timetable.db")
                    .Options;

                using var context = new TimetableDbContext(options);
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                ScheduleGrid.ItemsSource = null;
                StatusText.Text = "Adatbázis újraépítve (üres).";
                MessageBox.Show("Az adatbázis törölve és újra létrehozva.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportSeed_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var options = new DbContextOptionsBuilder<TimetableDbContext>()
                    .UseSqlite("Data Source=timetable.db")
                    .Options;

                using var context = new TimetableDbContext(options);
                context.Database.EnsureCreated();

                var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Seed", "test_school.json");
                JsonImporter.ImportFromFile(context, jsonPath, clearExisting: true);
                StatusText.Text = "Seed import kész.";
                MessageBox.Show("Seed import befejezve.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void WriteScheduleTextFile(Schedule? schedule, string outputPath)
        {
            using var writer = new StreamWriter(outputPath);
            if (schedule == null)
            {
                writer.WriteLine("(nincs ütemezés)");
                return;
            }

            foreach (var lesson in schedule.Lessons)
            {
                writer.WriteLine(
                    $"Lesson {lesson.Id}: {lesson.Subject.Name} with {lesson.Teacher.Name} for {lesson.ClassGroup.Name} at {lesson.AssignedTimeSlot?.Day} {lesson.AssignedTimeSlot?.Period} in {lesson.AssignedRoom?.Name}");
            }

            foreach (var unfilled in schedule.UnScheduledLessons)
            {
                writer.WriteLine(
                    $"Unscheduled: {unfilled.Subject.Name} for {unfilled.ClassGroup.Name} ({unfilled.Requirement.WeeklyHours} hours/week)");
            }

            writer.WriteLine($"Total Penalty: {schedule.TotalPenalty}");
        }

        private void RefreshScheduleGrid(Schedule? schedule)
        {
            _slotByColumn.Clear();
            _lessonsByCell.Clear();

            if (schedule == null || schedule.Lessons.Count == 0)
            {
                ScheduleGrid.ItemsSource = null;
                return;
            }

            var lessons = schedule.Lessons
                .Where(l =>
                    l.AssignedTimeSlot != null &&
                    l.ClassGroup != null &&
                    l.Subject != null &&
                    l.Teacher != null)
                .ToList();

            var slotKeys = lessons
                .Select(l => (l.AssignedTimeSlot!.Day, l.AssignedTimeSlot.Period))
                .Distinct()
                .OrderBy(x => x.Day)
                .ThenBy(x => x.Period)
                .ToList();

            if (slotKeys.Count == 0)
            {
                ScheduleGrid.ItemsSource = null;
                return;
            }

            var table = new DataTable();
            table.Columns.Add("Osztály", typeof(string));
            foreach (var (day, period) in slotKeys)
            {
                var caption = SlotColumnCaption(day, period);
                table.Columns.Add(caption, typeof(string));
                _slotByColumn[caption] = (day, period);
            }

            var lessonsByDisplayClass = lessons
                .SelectMany(lesson =>
                    GetDisplayClassNames(lesson).Select(className => new
                    {
                        ClassName = className,
                        Lesson = lesson
                    }))
                .ToList();

            foreach (var className in lessonsByDisplayClass.Select(x => x.ClassName).Distinct().OrderBy(n => n))
            {
                var row = table.NewRow();
                row["Osztály"] = className;
                foreach (var (day, period) in slotKeys)
                {
                    var caption = SlotColumnCaption(day, period);
                    var subject = lessonsByDisplayClass
                        .Where(x =>
                            x.ClassName == className &&
                            x.Lesson.AssignedTimeSlot!.Day == day &&
                            x.Lesson.AssignedTimeSlot.Period == period)
                        .Select(x => x.Lesson)
                        .GroupBy(l => l.Id)
                        .Select(g => g.First())
                        .ToList();

                    _lessonsByCell[(className, day, period)] = subject;
                    row[caption] = FormatCellDisplay(subject);
                }

                table.Rows.Add(row);
            }

            ScheduleGrid.ItemsSource = table.DefaultView;
        }

        private static IReadOnlyList<string> GetDisplayClassNames(Lesson lesson)
        {
            if (lesson.ClassGroup is Group group && group.Classes != null && group.Classes.Count > 0)
            {
                var classNames = group.Classes
                    .Select(c => c.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();

                if (classNames.Count > 0)
                {
                    return classNames;
                }
            }

            return [lesson.ClassGroup.Name];
        }

        private void ScheduleGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column is not DataGridTextColumn textColumn)
            {
                return;
            }

            textColumn.Binding = new Binding($"[{e.PropertyName}]")
            {
                Mode = BindingMode.OneWay,
                FallbackValue = string.Empty,
                TargetNullValue = string.Empty
            };

            var wrapStyle = new Style(typeof(TextBlock));
            wrapStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            wrapStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top));
            wrapStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 6, 8, 6)));
            wrapStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
            wrapStyle.Setters.Add(new Setter(TextBlock.LineHeightProperty, 18d));
            wrapStyle.Setters.Add(new Setter(TextBlock.LineStackingStrategyProperty, LineStackingStrategy.BlockLineHeight));
            textColumn.ElementStyle = wrapStyle;

            if (e.PropertyName == "Osztály")
            {
                e.Column.MinWidth = 110;
                e.Column.Width = new DataGridLength(1.15, DataGridLengthUnitType.Star);
            }
            else
            {
                e.Column.MinWidth = 150;
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }
        }

        private static string FormatCellDisplay(IReadOnlyList<Lesson> cellLessons)
        {
            if (cellLessons.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(Environment.NewLine + Environment.NewLine, cellLessons.Select(FormatSingleLessonDisplay));
        }

        private static string FormatSingleLessonDisplay(Lesson lesson)
        {
            var teacher = lesson.Teacher?.Name ?? "—";
            var room = lesson.AssignedRoom?.Name ?? "—";
            var subjectName = lesson.Subject?.Name ?? "—";
            return $"{subjectName}{Environment.NewLine}Terem: {room}{Environment.NewLine}Tanár: {teacher}";
        }

        private void ScheduleGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var clickedElement = e.OriginalSource as DependencyObject;
            var clickedCell = FindVisualParent<DataGridCell>(clickedElement);
            if (clickedCell == null || clickedCell.Column == null)
            {
                return;
            }

            var clickedRow = FindVisualParent<DataGridRow>(clickedCell);
            if (clickedRow?.Item is not DataRowView rowView)
            {
                return;
            }

            var header = clickedCell.Column.Header?.ToString() ?? string.Empty;
            if (!_slotByColumn.TryGetValue(header, out var slot))
            {
                return;
            }

            var className = rowView["Osztály"]?.ToString() ?? string.Empty;
            if (!_lessonsByCell.TryGetValue((className, slot.Day, slot.Period), out var lessons) || lessons.Count == 0)
            {
                MessageBox.Show(
                    "Ebben a cellában nincs ütemezett óra.",
                    "Órarészlet",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var details = new StringBuilder();
            for (var i = 0; i < lessons.Count; i++)
            {
                var lesson = lessons[i];
                if (i > 0)
                {
                    details.AppendLine();
                    details.AppendLine("-----");
                    details.AppendLine();
                }

                details.AppendLine($"Lesson ID: {lesson.Id}");
                details.AppendLine($"Osztály: {lesson.ClassGroup.Name}");
                details.AppendLine($"Tantárgy: {lesson.Subject.Name}");
                details.AppendLine($"Tanár: {lesson.Teacher.Name}");
                details.AppendLine($"Terem: {lesson.AssignedRoom?.Name ?? "-"}");
                details.AppendLine($"Idősáv: {header}");
                details.AppendLine($"Heti óraszám igény: {lesson.Requirement.WeeklyHours}");
            }

            MessageBox.Show(
                details.ToString(),
                $"Órarészlet - {className} ({header})",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T target)
                {
                    return target;
                }

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private static string SlotColumnCaption(int day, int period)
        {
            var d = day >= 0 && day < HungarianDayShort.Length ? HungarianDayShort[day] : $"N{day}";
            return $"{d} · {period}. óra";
        }
    }
}
