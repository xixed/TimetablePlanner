using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Generators
{
    internal class MAXSAT
    {
        private readonly IReadOnlyList<IHardConstraint> _hardConstraints;
        private readonly IReadOnlyList<ISoftConstraint> _softConstraints;
        private readonly IReadOnlyList<TimeSlot> _timeSlots;
        private readonly IReadOnlyList<Room> _rooms;

        public MAXSAT(
            IEnumerable<IHardConstraint> hardConstraints,
            IEnumerable<ISoftConstraint> softConstraints,
            IEnumerable<TimeSlot> timeSlots,
            IEnumerable<Room> rooms)
        {
            _hardConstraints = hardConstraints.ToList();
            _softConstraints = softConstraints.ToList();
            _timeSlots = timeSlots.ToList();
            _rooms = rooms.ToList();
        }

        /// <summary>
        /// Hard constrainteket mindig betartva próbálja a soft penalty összegét minimalizálni.
        /// Sztochasztikus lokális keresést (szimulált hűtés jellegű lépésekkel) alkalmaz.
        /// </summary>
        public Schedule Optimize(
            Schedule initialSchedule,
            int maxIterations = 20_000,
            double initialTemperature = 5.0,
            double coolingRate = 0.999,
            int parallelRuns = 0,
            IProgress<TimetablePlanner.Core.Models.GenerationProgressReport>? progress = null)
        {
            if (initialSchedule == null) throw new ArgumentNullException(nameof(initialSchedule));
            // Determine number of parallel runs
            var runs = parallelRuns <= 0 ? Environment.ProcessorCount : parallelRuns;

            // Launch independent workers and pick the best result
            var tasks = new Task<Schedule>[runs];
            for (var i = 0; i < runs; i++)
            {
                var seed = Environment.TickCount ^ (i * 397);
                var idx = i;
                tasks[idx] = Task.Run(() => OptimizeSingle(initialSchedule, maxIterations, initialTemperature, coolingRate, seed, idx, runs, progress));
            }

            Task.WaitAll(tasks);

            Schedule best = null;
            foreach (var t in tasks)
            {
                var s = t.Result;
                if (s == null) continue;
                if (best == null || s.TotalPenalty < best.TotalPenalty)
                {
                    best = s;
                }
            }

            return best ?? CloneSchedule(initialSchedule);
        }

        private Schedule OptimizeSingle(Schedule initialSchedule, int maxIterations, double initialTemperature, double coolingRate, int seed, int workerIndex, int totalWorkers, IProgress<TimetablePlanner.Core.Models.GenerationProgressReport>? progress)
        {
            var bestSchedule = CloneSchedule(initialSchedule);
            bestSchedule.TotalPenalty = ComputeTotalPenalty(bestSchedule);

            var current = CloneSchedule(bestSchedule);
            var random = new Random(seed);
            var temperature = initialTemperature;

            for (var iter = 0; iter < maxIterations; iter++)
            {
                if (temperature < 1e-4)
                {
                    break;
                }

                if (current.Lessons.Count == 0)
                {
                    break;
                }

                // Report progress occasionally (aggregate across workers)
                if (progress != null && iter % 100 == 0)
                {
                    var workerFraction = (double)iter / Math.Max(1, maxIterations);
                    var overall = ((double)workerIndex + workerFraction) / Math.Max(1, totalWorkers);
                    try
                    {
                        progress.Report(new TimetablePlanner.Core.Models.GenerationProgressReport
                        {
                            Overall = 0.3 + overall * 0.6, // map worker progress into MAXSAT portion [0.3..0.9]
                            Stage = "MAXSAT",
                            StageProgress = workerFraction
                        });
                    }
                    catch
                    {
                        // ignore progress failures
                    }
                }

                var lessonIndex = random.Next(current.Lessons.Count);
                var lesson = current.Lessons[lessonIndex];

                var originalSlot = lesson.AssignedTimeSlot;
                var originalRoom = lesson.AssignedRoom;

                var candidateSlots = lesson.Requirement?.PossibleTimeSlots?.Count > 0
                    ? lesson.Requirement.PossibleTimeSlots
                    : _timeSlots;

                var candidateRooms = lesson.Requirement?.SuitableRooms?.Count > 0
                    ? lesson.Requirement.SuitableRooms
                    : _rooms;

                if (candidateSlots == null || candidateSlots.Count == 0 || candidateRooms == null || candidateRooms.Count == 0)
                {
                    continue;
                }

                var slotCandidate = candidateSlots[random.Next(candidateSlots.Count)];
                var roomCandidate = candidateRooms[random.Next(candidateRooms.Count)];

                var oldPenalty = ComputeTotalPenalty(current);

                lesson.AssignedTimeSlot = slotCandidate;
                lesson.AssignedRoom = roomCandidate;

                if (!AllHardConstraintsSatisfied(current, lesson))
                {
                    lesson.AssignedTimeSlot = originalSlot;
                    lesson.AssignedRoom = originalRoom;
                    temperature *= coolingRate;
                    continue;
                }

                var newPenalty = ComputeTotalPenalty(current);
                var delta = newPenalty - oldPenalty;

                if (delta <= 0)
                {
                    if (newPenalty < bestSchedule.TotalPenalty)
                    {
                        bestSchedule = CloneSchedule(current);
                        bestSchedule.TotalPenalty = newPenalty;
                    }
                }
                else
                {
                    var acceptanceProb = Math.Exp(-delta / Math.Max(temperature, 1e-6));
                    if (random.NextDouble() > acceptanceProb)
                    {
                        lesson.AssignedTimeSlot = originalSlot;
                        lesson.AssignedRoom = originalRoom;
                    }
                }

                temperature *= coolingRate;
            }

            return bestSchedule;
        }

        private bool AllHardConstraintsSatisfied(Schedule schedule, Lesson changedLesson)
        {
            foreach (var hc in _hardConstraints)
            {
                if (!hc.IsSatisfied(schedule, changedLesson))
                {
                    return false;
                }
            }

            return true;
        }

        private int ComputeTotalPenalty(Schedule schedule)
        {
            var total = 0;
            foreach (var lesson in schedule.Lessons)
            {
                foreach (var sc in _softConstraints)
                {
                    total += sc.GetPenalty(schedule, lesson);
                }
            }

            return total;
        }

        private static Schedule CloneSchedule(Schedule source)
        {
            var clone = new Schedule
            {
                TotalPenalty = source.TotalPenalty
            };

            foreach (var lesson in source.Lessons)
            {
                clone.Lessons.Add(CloneLesson(lesson));
            }

            foreach (var u in source.UnScheduledLessons)
            {
                clone.UnScheduledLessons.Add(CloneLesson(u));
            }

            return clone;
        }

        private static Lesson CloneLesson(Lesson source)
        {
            // A Subject/Teacher/ClassGroup/Requirement referenciák változatlanok lehetnek,
            // de az AssignedTimeSlot/AssignedRoom értékeket külön példányként klónozzuk,
            // hogy a bestSchedule későbbi módosítások hatására se változzon.
            return new Lesson
            {
                Id = source.Id,
                Subject = source.Subject,
                Teacher = source.Teacher,
                ClassGroup = source.ClassGroup,
                AssignedRoom = source.AssignedRoom,
                AssignedTimeSlot = source.AssignedTimeSlot,
                Requirement = source.Requirement
            };
        }
    }
}
