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

            List<Schedule> result = new List<Schedule>();

            var schedule = generator.Generate(requirements);
            result.Add(schedule);

            return result;



        }
    }
}
