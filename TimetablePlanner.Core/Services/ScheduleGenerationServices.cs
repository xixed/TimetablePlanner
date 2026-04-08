using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                new Constraints.Soft_Constraints.LessonSplitOnSameDay()
            };

            var generators = new List<IGenerator>
            {
                new Generators.GreedyGenerator(hardConstraints),
                new Generators.GreedyPenaltyGenerator(hardConstraints, softConstraints)
            };

            List<Schedule> result = new List<Schedule>();

            foreach (var generator in generators)
            {
                var schedule = generator.Generate(requirements);
                result.Add(schedule);

            }

            

            

            

            return result;



        }
    }
}
