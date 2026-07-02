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
            var requirements = _repository.GetLessonRequirements();

            var lessonsGenerator = new LessonsGenerator();

            var hardConstraints = new List<IHardConstraint>
            {
                new Constraints.Hard_Constraints.ClassConflict(),
                new Constraints.Hard_Constraints.TeacherConflict(),
                new Constraints.Hard_Constraints.RoomConflict(),
                new Constraints.Hard_Constraints.TeacherUnavailableTimeSlot()

            };

            var softConstraints = new List<ISoftConstraint>
            {
                new Constraints.Soft_Constraints.DoubleLessonPreference(),
                new Constraints.Soft_Constraints.LessonSplitOnSameDay(),
                new Constraints.Soft_Constraints.TeacherGap(),
                new Constraints.Soft_Constraints.ClassGap(),
                new Constraints.Soft_Constraints.RoomStability(),
                new Constraints.Soft_Constraints.TeacherOneLessonAvoidance()
            };

            var generator = new Generators.GreedyGenerator(hardConstraints, softConstraints, lessonsGenerator);
            
            // 1) Gyors inicializálás Greedy-vel (ez garantálja a kezdeti hard constraint-megfelelést).
            var initialSchedule = generator.Generate(requirements);

            // 2) MAX-SAT jellegű finomhangolás: hard constraint-eket megtartjuk, soft constraint-eket minimalizáljuk.
            var maxsat = new MAXSAT(
                hardConstraints,
                softConstraints,
                _repository.GetTimeSlots(),
                _repository.GetRooms());

            var optimizedSchedule = maxsat.Optimize(initialSchedule, maxIterations: 8_000);
            
            // Score-ot konzisztensen számolunk a soft constraint-ek alapján.
            // (Greedy a rész-ütemezés során kalkulálhat, ezért érdemes újraszámolni a teljes végső schedule-re.)
            var initialScore = SumSoftPenalty(initialSchedule, softConstraints);
            var optimizedScore = SumSoftPenalty(optimizedSchedule, softConstraints);

            initialSchedule.TotalPenalty = initialScore;
            optimizedSchedule.TotalPenalty = optimizedScore;

            var finalSchedule = optimizedScore <= initialScore ? optimizedSchedule : initialSchedule;
            return new List<Schedule> { finalSchedule };



        }

        private static int SumSoftPenalty(Schedule schedule, List<ISoftConstraint> softConstraints)
        {
            var total = 0;
            foreach (var lesson in schedule.Lessons)
            {
                foreach (var sc in softConstraints)
                {
                    total += sc.GetPenalty(schedule, lesson);
                }
            }

            return total;
        }
    }
}
