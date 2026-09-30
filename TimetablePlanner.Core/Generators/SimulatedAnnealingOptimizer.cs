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
        /// (hard constraint-ek mindig teljesülnek). A hőmérséklet a megadott időkeret (vagy az iterációszám)
        /// előrehaladásával csökken geometrikusan, így a keresés végigfut a teljes kereten.
        /// </summary>
        public Schedule Optimize(
            List<LessonRequirement> requirements,
            TimeSpan timeLimit,
            int maxIterations = 1_000_000,
            double initialTemperature = 20.0,
            double finalTemperature = 0.3,
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
                    requirements, timeLimit, maxIterations, initialTemperature, finalTemperature,
                    seed, idx, runs, progress));
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
            TimeSpan timeLimit,
            int maxIterations,
            double initialTemperature,
            double finalTemperature,
            int seed,
            int workerIndex,
            int totalWorkers,
            IProgress<GenerationProgressReport>? progress)
        {
            var random = new Random(seed);

            // Saját kezdőmegoldás (nem a greedy-é).
            var current = BuildRandomSchedule(requirements, random);
            var lessons = current.Lessons.ToArray();
            if (lessons.Length == 0)
            {
                return current;
            }

            // Egy lépés csak a mozgatott óra osztályát és tanárát érinti, ezért csak azok óráit pontozzuk újra.
            var byClass = new Dictionary<int, List<Lesson>>();
            var byTeacher = new Dictionary<int, List<Lesson>>();
            foreach (var l in lessons)
            {
                if (l.ClassGroup != null)
                {
                    AddTo(byClass, l.ClassGroup.Id, l);
                }
                if (l.Teacher != null)
                {
                    AddTo(byTeacher, l.Teacher.Id, l);
                }
            }

            var affected = new HashSet<Lesson>(ReferenceEqualityComparer.Instance);

            void Collect(Lesson l)
            {
                if (l.ClassGroup != null && byClass.TryGetValue(l.ClassGroup.Id, out var c))
                {
                    affected.UnionWith(c);
                }
                if (l.Teacher != null && byTeacher.TryGetValue(l.Teacher.Id, out var t))
                {
                    affected.UnionWith(t);
                }
            }

            int LocalPenalty(Lesson a, Lesson? b)
            {
                affected.Clear();
                Collect(a);
                if (b != null)
                {
                    Collect(b);
                }

                return ComputeTotalPenalty(new Schedule { Lessons = affected.ToList() });
            }

            var currentPenalty = ComputeTotalPenalty(current);
            var bestPenalty = currentPenalty;
            var bestSlots = lessons.Select(l => l.AssignedTimeSlot).ToArray();
            var bestRooms = lessons.Select(l => l.AssignedRoom).ToArray();

            void SaveBest()
            {
                for (var i = 0; i < lessons.Length; i++)
                {
                    bestSlots[i] = lessons[i].AssignedTimeSlot;
                    bestRooms[i] = lessons[i].AssignedRoom;
                }
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var limitSeconds = Math.Max(0.001, timeLimit.TotalSeconds);
            var coolingRatio = finalTemperature / initialTemperature;

            for (var iter = 0; iter < maxIterations; iter++)
            {
                // A keret előrehaladása: az iterációszám vagy az idő, amelyik előrébb tart.
                var fraction = Math.Max((double)iter / maxIterations, stopwatch.Elapsed.TotalSeconds / limitSeconds);
                if (fraction >= 1.0)
                {
                    break;
                }

                // Geometrikus hűtés a teljes kereten, a lépések kimenetelétől függetlenül.
                var temperature = initialTemperature * Math.Pow(coolingRatio, fraction);

                if (progress != null && iter % 100 == 0)
                {
                    var overall = ((double)workerIndex + fraction) / Math.Max(1, totalWorkers);
                    try
                    {
                        progress.Report(new GenerationProgressReport
                        {
                            Overall = 0.3 + overall * 0.2, // a worker haladása a [0.3..0.5] sávba képezve
                            Stage = "Szimulált hűtés",
                            StageProgress = fraction
                        });
                    }
                    catch
                    {
                        // ignore progress failures
                    }
                }

                var lesson = lessons[random.Next(lessons.Length)];

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

                // Változatlan hely: nincs mit kiértékelni.
                if (ReferenceEquals(slotCandidate, originalSlot) && ReferenceEquals(roomCandidate, originalRoom))
                {
                    continue;
                }

                var before = LocalPenalty(lesson, null);

                lesson.AssignedTimeSlot = slotCandidate;
                lesson.AssignedRoom = roomCandidate;

                if (!AllHardConstraintsSatisfied(current, lesson))
                {
                    lesson.AssignedTimeSlot = originalSlot;
                    lesson.AssignedRoom = originalRoom;
                    continue;
                }

                var delta = LocalPenalty(lesson, null) - before;

                if (delta <= 0 || random.NextDouble() <= Math.Exp(-delta / Math.Max(temperature, 1e-6)))
                {
                    currentPenalty += delta;
                    if (currentPenalty < bestPenalty)
                    {
                        bestPenalty = currentPenalty;
                        SaveBest();
                    }
                }
                else
                {
                    lesson.AssignedTimeSlot = originalSlot;
                    lesson.AssignedRoom = originalRoom;
                }

                // Csere-lépés: két óra helyének felcserélése
                if (lessons.Length > 1 && random.NextDouble() < 0.3)
                {
                    var a = lessons[random.Next(lessons.Length)];
                    var b = lessons[random.Next(lessons.Length)];

                    if (ReferenceEquals(a, b)
                        || !CanUse(a, b.AssignedTimeSlot, b.AssignedRoom)
                        || !CanUse(b, a.AssignedTimeSlot, a.AssignedRoom))
                    {
                        continue;
                    }

                    var swapBefore = LocalPenalty(a, b);

                    (a.AssignedTimeSlot, b.AssignedTimeSlot) = (b.AssignedTimeSlot, a.AssignedTimeSlot);
                    (a.AssignedRoom, b.AssignedRoom) = (b.AssignedRoom, a.AssignedRoom);

                    var accept = false;
                    var swapDelta = 0;
                    if (AllHardConstraintsSatisfied(current, a, b))
                    {
                        swapDelta = LocalPenalty(a, b) - swapBefore;
                        accept = swapDelta <= 0
                            || random.NextDouble() <= Math.Exp(-(double)swapDelta / Math.Max(temperature, 1e-6));
                    }

                    if (!accept)
                    {
                        (a.AssignedTimeSlot, b.AssignedTimeSlot) = (b.AssignedTimeSlot, a.AssignedTimeSlot);
                        (a.AssignedRoom, b.AssignedRoom) = (b.AssignedRoom, a.AssignedRoom);
                    }
                    else
                    {
                        currentPenalty += swapDelta;
                        if (currentPenalty < bestPenalty)
                        {
                            bestPenalty = currentPenalty;
                            SaveBest();
                        }
                    }
                }
            }

            // Ha a növekményes számítás eltér a teljestől, valamelyik constraint nem bontható osztály/tanár szerint.
            System.Diagnostics.Debug.Assert(
                currentPenalty == ComputeTotalPenalty(current),
                "A növekményes büntetés eltér a teljes újraszámolástól.");

            // A legjobb állapot visszaállítása és pontos újraszámolás.
            for (var i = 0; i < lessons.Length; i++)
            {
                lessons[i].AssignedTimeSlot = bestSlots[i];
                lessons[i].AssignedRoom = bestRooms[i];
            }

            current.TotalPenalty = ComputeTotalPenalty(current);
            return current;
        }

        private static void AddTo(Dictionary<int, List<Lesson>> map, int key, Lesson lesson)
        {
            if (!map.TryGetValue(key, out var list))
            {
                map[key] = list = new List<Lesson>();
            }
            list.Add(lesson);
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
