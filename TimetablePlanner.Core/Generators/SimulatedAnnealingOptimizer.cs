using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimetablePlanner.Core.Interfaces.Constraint;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Generators
{
    /// <summary>
    /// Szimulált hűtésen alapuló ütemezés-optimalizáló. Saját, véletlenszerű kezdőmegoldásból indul,
    /// és a hard constraint-ek megsértése nélkül minimalizálja a soft büntetések összegét
    /// (áthelyezés és csere lépésekkel). A greedy eredmény csak referencia:
    /// ha egy futás a türelmi idő után sem javul, és még mindig nem jobb a referenciánál, leáll.
    /// </summary>
    internal class SimulatedAnnealingOptimizer
    {
        private readonly IReadOnlyList<IHardConstraint> _hardConstraints;
        private readonly IReadOnlyList<ISoftConstraint> _softConstraints;
        private readonly IReadOnlyList<TimeSlot> _timeSlots;
        private readonly IReadOnlyList<Room> _rooms;

        public SimulatedAnnealingOptimizer(
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
        /// Saját, véletlenszerű kezdőmegoldásból indulva minimalizálja a soft penalty összegét
        /// (hard constraint-ek mindig teljesülnek). A greedy eredmény csak referencia:
        /// ha egy worker a türelmi idő után sem javul, és még mindig nem jobb a referenciánál, leáll.
        /// </summary>
        public Schedule Optimize(
            List<LessonRequirement> requirements,
            int referencePenalty,
            int referenceUnscheduledCount,
            int maxIterations = 100_000,
            double initialTemperature = 5.0,
            double coolingRate = 0.999,
            int parallelRuns = 0,
            IProgress<GenerationProgressReport>? progress = null)
        {
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));

            var runs = parallelRuns <= 0 ? Environment.ProcessorCount : parallelRuns;

            var tasks = new Task<Schedule>[runs];
            for (var i = 0; i < runs; i++)
            {
                var seed = Environment.TickCount ^ (i * 397);
                var idx = i;
                tasks[idx] = Task.Run(() => OptimizeSingle(
                    requirements, referencePenalty, referenceUnscheduledCount,
                    maxIterations, initialTemperature, coolingRate, seed, idx, runs, progress));
            }

            Task.WaitAll(tasks);

            Schedule? best = null;
            foreach (var t in tasks)
            {
                var s = t.Result;
                if (s == null) continue;
                if (best == null
                    || s.UnScheduledLessons.Count < best.UnScheduledLessons.Count
                    || (s.UnScheduledLessons.Count == best.UnScheduledLessons.Count && s.TotalPenalty < best.TotalPenalty))
                {
                    best = s;
                }
            }

            return best ?? new Schedule();
        }

        private Schedule OptimizeSingle(
            List<LessonRequirement> requirements,
            int referencePenalty,
            int referenceUnscheduledCount,
            int maxIterations,
            double initialTemperature,
            double coolingRate,
            int seed,
            int workerIndex,
            int totalWorkers,
            IProgress<GenerationProgressReport>? progress)
        {
            var random = new Random(seed);

            // Saját kezdőmegoldás (nem a greedy-é).
            var current = BuildRandomSchedule(requirements, random);
            var bestSchedule = CloneSchedule(current);
            bestSchedule.TotalPenalty = ComputeTotalPenalty(bestSchedule);

            var temperature = initialTemperature;

            // Korai leállítás paraméterei
            var minIterations = Math.Max(200, maxIterations / 10);
            var patience = Math.Max(200, maxIterations / 4);
            var lastImprovementIter = 0;

            for (var iter = 0; iter < maxIterations; iter++)
            {
                if (temperature < 1e-4 || current.Lessons.Count == 0)
                {
                    break;
                }

                // Ha már láthatóan nem tud jobb lenni a referenciánál, leállunk.
                if (iter >= minIterations
                    && iter - lastImprovementIter >= patience
                    && !IsAtLeastAsGoodAs(bestSchedule, referencePenalty, referenceUnscheduledCount))
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
                        progress.Report(new GenerationProgressReport
                        {
                            Overall = 0.3 + overall * 0.6, // a worker haladása a [0.3..0.9] sávba képezve
                            Stage = "Szimulált hűtés",
                            StageProgress = workerFraction
                        });
                    }
                    catch
                    {
                        // ignore progress failures
                    }
                }

                var lesson = current.Lessons[random.Next(current.Lessons.Count)];

                var originalSlot = lesson.AssignedTimeSlot;
                var originalRoom = lesson.AssignedRoom;

                var candidateSlots = GetCandidateSlots(lesson);
                var candidateRooms = GetCandidateRooms(lesson);

                if (candidateSlots.Count == 0 || candidateRooms.Count == 0)
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
                        lastImprovementIter = iter;
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

                // Csere-lépés: két óra helyének felcserélése
                if (current.Lessons.Count > 1 && random.NextDouble() < 0.3)
                {
                    var a = current.Lessons[random.Next(current.Lessons.Count)];
                    var b = current.Lessons[random.Next(current.Lessons.Count)];

                    if (ReferenceEquals(a, b)
                        || !CanUse(a, b.AssignedTimeSlot, b.AssignedRoom)
                        || !CanUse(b, a.AssignedTimeSlot, a.AssignedRoom))
                    {
                        temperature *= coolingRate;
                        continue;
                    }

                    var swapOldPenalty = ComputeTotalPenalty(current);

                    (a.AssignedTimeSlot, b.AssignedTimeSlot) = (b.AssignedTimeSlot, a.AssignedTimeSlot);
                    (a.AssignedRoom, b.AssignedRoom) = (b.AssignedRoom, a.AssignedRoom);

                    var swapOk = AllHardConstraintsSatisfied(current, a, b);
                    var swapNewPenalty = swapOk ? ComputeTotalPenalty(current) : int.MaxValue;
                    var swapDelta = swapNewPenalty - swapOldPenalty;

                    var accept = swapOk
                        && (swapDelta <= 0
                            || random.NextDouble() <= Math.Exp(-(double)swapDelta / Math.Max(temperature, 1e-6)));

                    if (!accept)
                    {
                        (a.AssignedTimeSlot, b.AssignedTimeSlot) = (b.AssignedTimeSlot, a.AssignedTimeSlot);
                        (a.AssignedRoom, b.AssignedRoom) = (b.AssignedRoom, a.AssignedRoom);
                    }
                    else if (swapNewPenalty < bestSchedule.TotalPenalty)
                    {
                        bestSchedule = CloneSchedule(current);
                        bestSchedule.TotalPenalty = swapNewPenalty;
                        lastImprovementIter = iter;
                    }

                    temperature *= coolingRate;
                    continue;
                }
            }

            return bestSchedule;
        }

        private static bool IsAtLeastAsGoodAs(Schedule schedule, int referencePenalty, int referenceUnscheduledCount)
        {
            if (schedule.UnScheduledLessons.Count != referenceUnscheduledCount)
            {
                return schedule.UnScheduledLessons.Count < referenceUnscheduledCount;
            }

            return schedule.TotalPenalty <= referencePenalty;
        }

        /// <summary>
        /// Véletlenszerű sorrendben, véletlenszerű (slot, room) párokkal épít hard constraint-ekre érvényes ütemezést.
        /// </summary>
        private Schedule BuildRandomSchedule(List<LessonRequirement> requirements, Random random)
        {
            var schedule = new Schedule();
            var lessons = new LessonsGenerator().Generate(requirements)
                .OrderBy(_ => random.Next())
                .ToList();

            foreach (var lesson in lessons)
            {
                var slots = GetCandidateSlots(lesson);
                var rooms = GetCandidateRooms(lesson);

                var pairs = new List<(TimeSlot Slot, Room Room)>();
                foreach (var s in slots)
                {
                    foreach (var r in rooms)
                    {
                        pairs.Add((s, r));
                    }
                }

                // Fisher–Yates keverés
                for (var i = pairs.Count - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    (pairs[i], pairs[j]) = (pairs[j], pairs[i]);
                }

                var placed = false;
                foreach (var (slot, room) in pairs)
                {
                    lesson.AssignedTimeSlot = slot;
                    lesson.AssignedRoom = room;

                    if (AllHardConstraintsSatisfied(schedule, lesson))
                    {
                        schedule.AddLesson(lesson);
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    lesson.AssignedTimeSlot = null;
                    lesson.AssignedRoom = null;
                    schedule.UnScheduledLessons.Add(lesson);
                }
            }

            schedule.TotalPenalty = ComputeTotalPenalty(schedule);
            return schedule;
        }

        private IReadOnlyList<TimeSlot> GetCandidateSlots(Lesson lesson)
        {
            return lesson.Requirement?.PossibleTimeSlots?.Count > 0
                ? lesson.Requirement.PossibleTimeSlots
                : _timeSlots;
        }

        private IReadOnlyList<Room> GetCandidateRooms(Lesson lesson)
        {
            return lesson.Requirement?.SuitableRooms?.Count > 0
                ? lesson.Requirement.SuitableRooms
                : _rooms;
        }

        /// <summary>
        /// Egy óra hard constraint-ellenőrzése úgy, hogy az óra saját magával ne ütközzön.
        /// </summary>
        private bool AllHardConstraintsSatisfied(Schedule schedule, Lesson changedLesson)
        {
            var removed = schedule.Lessons.Remove(changedLesson);
            try
            {
                return CheckHard(schedule, changedLesson);
            }
            finally
            {
                if (removed)
                {
                    schedule.Lessons.Add(changedLesson);
                }
            }
        }

        /// <summary>
        /// Két óra egyidejű mozgatásának (cseréjének) hard constraint-ellenőrzése.
        /// </summary>
        private bool AllHardConstraintsSatisfied(Schedule schedule, Lesson a, Lesson b)
        {
            var removedA = schedule.Lessons.Remove(a);
            var removedB = schedule.Lessons.Remove(b);
            try
            {
                if (!CheckHard(schedule, a))
                {
                    return false;
                }

                schedule.Lessons.Add(a);
                try
                {
                    return CheckHard(schedule, b);
                }
                finally
                {
                    schedule.Lessons.Remove(a);
                }
            }
            finally
            {
                if (removedA) schedule.Lessons.Add(a);
                if (removedB) schedule.Lessons.Add(b);
            }
        }

        private bool CheckHard(Schedule schedule, Lesson lesson)
        {
            foreach (var hc in _hardConstraints)
            {
                if (!hc.IsSatisfied(schedule, lesson))
                {
                    return false;
                }
            }

            return true;
        }

        private int ComputeTotalPenalty(Schedule schedule)
        {
            var total = 0;
            foreach (var sc in _softConstraints)
            {
                total += sc.GetTotalPenalty(schedule);
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

        private bool CanUse(Lesson lesson, TimeSlot? slot, Room? room)
        {
            return slot != null
                && room != null
                && GetCandidateSlots(lesson).Contains(slot)
                && GetCandidateRooms(lesson).Contains(room);
        }
    }
}
