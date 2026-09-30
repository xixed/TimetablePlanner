using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimetablePlanner.Core.Generators;
using TimetablePlanner.Core.Interfaces;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;



namespace TimetablePlanner.Core.Services
{
    public class ScheduleGenerationServices : IScheduleGenerationService
    {
        private readonly ISchoolRepository _repository;

        public ScheduleGenerationServices(ISchoolRepository schoolRepository)
        {
            _repository = schoolRepository;
        }

        public List<Schedule> GenerateSchedule()
        {
            return GenerateSchedule(null);
        }

        public List<Schedule> GenerateSchedule(IProgress<TimetablePlanner.Core.Models.GenerationProgressReport>? progress = null)
        {
            var requirements = _repository.GetLessonRequirements();

            var lessonsGenerator = new LessonsGenerator();

            var hardConstraints = new List<IHardConstraint>
            {
                new Constraints.Hard_Constraints.ClassConflict(),
                new Constraints.Hard_Constraints.TeacherConflict(),
                new Constraints.Hard_Constraints.RoomConflict(),
                new Constraints.Hard_Constraints.TeacherUnavailableTimeSlot(),
                new Constraints.Hard_Constraints.RoomCapacity()

            };

            var softConstraints = new List<ISoftConstraint>
            {
                new Constraints.Soft_Constraints.DoubleLessonPreference(),
                new Constraints.Soft_Constraints.LessonSplitOnSameDay(),
                new Constraints.Soft_Constraints.TeacherGap(),
                new Constraints.Soft_Constraints.ClassGap(),
                new Constraints.Soft_Constraints.RoomStability(),
                new Constraints.Soft_Constraints.TeacherOneLessonAvoidance(),
                new Constraints.Soft_Constraints.ClassDayStart(),
                new Constraints.Soft_Constraints.LateLesson(),
                new Constraints.Soft_Constraints.UnscheduledLessonPenalty(),
                new Constraints.Soft_Constraints.ClassDayBalance(),
                
            };

            var generator = new Generators.GreedyGenerator(hardConstraints, softConstraints, lessonsGenerator);

            // 1) Greedy: gyors, lehetséges megoldás, amely referenciaként szolgál.
            progress?.Report(new GenerationProgressReport { Overall = 0.05, Stage = "Greedy", StageProgress = 0.0 });
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var greedySchedule = generator.Generate(requirements);
            greedySchedule.Duration = sw.Elapsed;
            greedySchedule.GeneratorName = "Greedy";
            var greedyScore = SumSoftPenalty(greedySchedule, softConstraints);
            greedySchedule.TotalPenalty = greedyScore;
            progress?.Report(new GenerationProgressReport { Overall = 0.3, Stage = "Greedy", StageProgress = 1.0 });

            // 2) Szimulált hűtés: saját kezdőmegoldásból indul, a greedy eredményét csak összehasonlításra kapja.
            //    Ha láthatóan nem tud jobbat adni a greedy-nél, idő előtt leáll.
            var optimizer = new SimulatedAnnealingOptimizer(
                hardConstraints,
                softConstraints,
                _repository.GetTimeSlots(),
                _repository.GetRooms());

            IProgress<GenerationProgressReport>? wrapped = null;
            if (progress != null)
            {
                wrapped = new Progress<GenerationProgressReport>(r => progress.Report(r));
            }

            sw.Restart();
            var annealedSchedule = optimizer.Optimize(
                requirements,
                timeLimit: TimeSpan.FromSeconds(120),
                progress: wrapped);
            annealedSchedule.Duration = sw.Elapsed;

            annealedSchedule.GeneratorName = "Szimulált hűtés";
            annealedSchedule.TotalPenalty = SumSoftPenalty(annealedSchedule, softConstraints);

            // 3) CP-SAT: a Greedy és a szimulált hűtés jobbik eredményét kapja kezdőmegoldásként.
            Schedule? cpSatSchedule = null;
            progress?.Report(new GenerationProgressReport { Overall = 0.5, Stage = "CP-SAT", StageProgress = 0.0 });
            try
            {
                var cpSat = new CpSatGenerator(lessonsGenerator, timeLimitSeconds: 120);
                var hintSchedule = IsBetter(annealedSchedule, greedySchedule) ? annealedSchedule : greedySchedule;
                sw.Restart();
                cpSatSchedule = cpSat.Generate(requirements, hintSchedule, progress);
                cpSatSchedule.Duration = sw.Elapsed;
                cpSatSchedule.GeneratorName = "CP-SAT (hint: " + hintSchedule.GeneratorName + ")";
                cpSatSchedule.TotalPenalty = SumSoftPenalty(cpSatSchedule, softConstraints);

                foreach (var sc in softConstraints)
                {
                    System.Diagnostics.Debug.WriteLine($"  {sc.Name}: {sc.GetTotalPenalty(cpSatSchedule)}");
                }
            }
            catch (Exception)
            {
                // A CP-SAT hibája ne akadályozza a többi generátor eredményét.
                cpSatSchedule = null;
            }
            progress?.Report(new GenerationProgressReport { Overall = 0.95, Stage = "CP-SAT", StageProgress = 1.0 });

            // Összehasonlítás: először a kevesebb be nem osztott óra, utána a kisebb büntetés.
            var candidates = new List<Schedule> { greedySchedule, annealedSchedule };
            if (cpSatSchedule != null)
            {
                candidates.Add(cpSatSchedule);
            }

            var finalSchedule = candidates.Aggregate((best, next) => IsBetter(next, best) ? next : best);

            progress?.Report(new GenerationProgressReport { Overall = 1.0, Stage = "Complete", StageProgress = 1.0 });

            // Az első elem a legjobb, utána a többi generátor eredménye összehasonlításhoz.
            var result = new List<Schedule> { finalSchedule };
            result.AddRange(candidates.Where(c => !ReferenceEquals(c, finalSchedule)));
            return result;
        }

        private static int SumSoftPenalty(Schedule schedule, List<ISoftConstraint> softConstraints)
        {
            var total = 0;
            foreach (var sc in softConstraints)
            {
                total += sc.GetTotalPenalty(schedule);
            }

            return total;
        }

        private static bool IsBetter(Schedule candidate, Schedule current)
        {
            if (candidate.UnScheduledLessons.Count != current.UnScheduledLessons.Count)
            {
                return candidate.UnScheduledLessons.Count < current.UnScheduledLessons.Count;
            }

            return candidate.TotalPenalty < current.TotalPenalty;
        }
    }
}
