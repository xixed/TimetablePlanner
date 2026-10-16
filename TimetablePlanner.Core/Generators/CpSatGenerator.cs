using Google.OrTools.Sat;
using TimetablePlanner.Core.Constraints.Soft_Constraints;
using TimetablePlanner.Core.Models;

namespace TimetablePlanner.Core.Generators
{
    public class CpSatGenerator
    {
        private readonly LessonsGenerator _lessonsGenerator;
        private readonly int _timeLimitSeconds;

        public CpSatGenerator(LessonsGenerator lessonsGenerator, int timeLimitSeconds = 30)
        {
            _lessonsGenerator = lessonsGenerator;
            _timeLimitSeconds = timeLimitSeconds;
        }

        public Schedule Generate(
            List<LessonRequirement> requirements,
            Schedule? hint = null,
            IProgress<GenerationProgressReport>? progress = null)
        {
            var allSlots = requirements.SelectMany(r => r.PossibleTimeSlots).ToList();
            if (allSlots.Count == 0)
            {
                var empty = new Schedule();
                empty.UnScheduledLessons.AddRange(_lessonsGenerator.Generate(requirements));
                return empty;
            }

            var minPeriod = allSlots.Min(s => s.Period);
            var maxPeriod = allSlots.Max(s => s.Period);
            var periods = Enumerable.Range(minPeriod, maxPeriod - minPeriod + 1).ToArray();

            var model = new CpModel();
            var zero = model.NewConstant(0);
            var terms = new List<LinearExpr>();

            var placements = new List<Placement>();
            var reqSlots = new Dictionary<LessonRequirement, Dictionary<int, (TimeSlot Slot, BoolVar X)>>();

            var byTeacherSlot = new Dictionary<(int, int), List<BoolVar>>();
            var byRoomSlot = new Dictionary<(int, int), List<BoolVar>>();
            var byClassUnitSlot = new Dictionary<(string, int), List<BoolVar>>();

            var classDay = new Dictionary<(int Id, int Day), Dictionary<int, List<BoolVar>>>();
            var teacherDay = new Dictionary<(int Id, int Day), Dictionary<int, List<BoolVar>>>();
            var classDayRoom = new Dictionary<(int ClassId, int Day, int RoomId), List<BoolVar>>();
            var classSubjectDay = new Dictionary<(int ClassId, int SubjectId, int Day), Dictionary<int, List<BoolVar>>>();
            var parallelSubjectSlot = new Dictionary<(string ParallelKey, int SubjectId, int SlotKey), List<BoolVar>>(StringTupleComparer.OrdinalIgnoreCaseInstance);

            foreach (var req in requirements)
            {
                var slotMap = new Dictionary<int, (TimeSlot, BoolVar)>();
                reqSlots[req] = slotMap;

                foreach (var slot in req.PossibleTimeSlots.GroupBy(SlotKey).Select(g => g.First()))
                {
                    int slotKey = SlotKey(slot);

                    if (req.Teacher?.UnavailableTimeSlots != null &&
                        req.Teacher.UnavailableTimeSlots.Any(u => u.Day == slot.Day && u.Period == slot.Period))
                    {
                        continue;
                    }

                    var roomVars = new List<BoolVar>();
                    foreach (var room in req.SuitableRooms)
                    {
                        if (req.ClassGroup != null && room.Capacity < req.ClassGroup.Size)
                        {
                            continue;
                        }

                        var y = model.NewBoolVar($"y_{req.Id}_{slotKey}_{room.Id}");
                        placements.Add(new Placement(req, slot, room, y));
                        roomVars.Add(y);
                        Add(byRoomSlot, (room.Id, slotKey), y);

                        if (req.ClassGroup != null)
                        {
                            Add(classDayRoom, (req.ClassGroup.Id, slot.Day, room.Id), y);
                        }
                    }

                    if (roomVars.Count == 0)
                    {
                        continue;
                    }

                    BoolVar x;
                    if (roomVars.Count == 1)
                    {
                        x = roomVars[0];
                    }
                    else
                    {
                        x = model.NewBoolVar($"x_{req.Id}_{slotKey}");
                        model.Add(x == LinearExpr.Sum(roomVars));
                    }
                    slotMap[slotKey] = (slot, x);

                    Add(byTeacherSlot, (req.Teacher?.Id ?? -1, slotKey), x);
                    foreach (var unit in ConflictUnits(req.ClassGroup))
                    {
                        Add(byClassUnitSlot, (unit, slotKey), x);
                    }

                    if (req.ClassGroup != null)
                    {
                        AddPeriod(classDay, (req.ClassGroup.Id, slot.Day), slot.Period, x);
                    }
                    if (req.Teacher != null)
                    {
                        AddPeriod(teacherDay, (req.Teacher.Id, slot.Day), slot.Period, x);
                    }
                    if (req.ClassGroup != null && req.Subject != null)
                    {
                        AddPeriod(classSubjectDay, (req.ClassGroup.Id, req.Subject.Id, slot.Day), slot.Period, x);
                    }
                    if (req.ClassGroup is Group group && req.Subject != null && !string.IsNullOrWhiteSpace(group.ParallelKey))
                    {
                        Add(parallelSubjectSlot, (group.ParallelKey!, req.Subject.Id, slotKey), x);
                    }

                    var late = Math.Max(0, slot.Period - LateLesson.LastNormalPeriod) * LateLesson.PenaltyPerPeriod;
                    if (late > 0)
                    {
                        terms.Add(x * late);
                    }
                }

                var scheduled = slotMap.Values.Select(v => v.Item2).ToList();
                model.Add(LinearExpr.Sum(scheduled) <= req.WeeklyHours);

                var unscheduled = model.NewIntVar(0, req.WeeklyHours, $"u_{req.Id}");
                model.Add(LinearExpr.Sum(scheduled) + unscheduled == req.WeeklyHours);
                terms.Add(unscheduled * UnscheduledLessonPenalty.PenaltyPerLesson);

                AddDoubleLessonTerms(model, req, slotMap, terms);
            }

            AddAtMostOne(model, byTeacherSlot.Values);
            AddAtMostOne(model, byRoomSlot.Values);
            AddAtMostOne(model, byClassUnitSlot.Values);

            var classUsed = AddOccupancyTerms(
                model, zero, classDay, periods, ClassGap.PenaltyPerGap, ClassDayStart.PenaltyPerPeriod, 0, terms);
            AddOccupancyTerms(
                model, zero, teacherDay, periods, TeacherGap.PenaltyPerGap, 0, TeacherOneLessonAvoidance.Penalty, terms);

            foreach (var group in classDayRoom.GroupBy(kv => (kv.Key.ClassId, kv.Key.Day)))
            {
                if (!classUsed.TryGetValue((group.Key.ClassId, group.Key.Day), out var used))
                {
                    continue;
                }

                var roomUsedVars = new List<BoolVar>();
                foreach (var (key, ys) in group)
                {
                    var ru = model.NewBoolVar($"ru_{key.ClassId}_{key.Day}_{key.RoomId}");
                    foreach (var y in ys)
                    {
                        model.Add(y <= ru);
                    }
                    model.Add(ru <= LinearExpr.Sum(ys));
                    roomUsedVars.Add(ru);
                }

                var roomCount = model.NewIntVar(0, roomUsedVars.Count, $"rc_{group.Key.ClassId}_{group.Key.Day}");
                model.Add(roomCount == LinearExpr.Sum(roomUsedVars));

                var extraRooms = model.NewIntVar(0, roomUsedVars.Count, $"er_{group.Key.ClassId}_{group.Key.Day}");
                model.Add(extraRooms >= roomCount - used);
                model.Add(extraRooms <= roomCount);
                model.Add(extraRooms <= roomUsedVars.Count * used);
                terms.Add(extraRooms * RoomStability.PenaltyPerExtraRoom);
            }

            AddClassDayBalanceTerms(model, zero, classDay, terms);
            AddLessonSplitTerms(model, classSubjectDay, terms);
            AddParallelGroupSynchronizationTerms(model, parallelSubjectSlot, terms);

            if (hint != null)
            {
                var hinted = new HashSet<(int ReqId, int SlotKey, int RoomId)>(
                    hint.Lessons
                        .Where(l => l.AssignedTimeSlot != null && l.AssignedRoom != null && l.Requirement != null)
                        .Select(l => (l.Requirement.Id, SlotKey(l.AssignedTimeSlot!), l.AssignedRoom!.Id)));

                foreach (var p in placements)
                {
                    model.AddHint(p.Var, hinted.Contains((p.Requirement.Id, SlotKey(p.Slot), p.Room.Id)) ? 1 : 0);
                }
            }

            model.Minimize(LinearExpr.Sum(terms));

            var solver = new CpSolver
            {
                StringParameters =
                    $"max_time_in_seconds:{_timeLimitSeconds} num_workers:{Math.Max(1, Environment.ProcessorCount)} " +
                    "repair_hint:true linearization_level:2 symmetry_level:2"
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using var progressTimer = new System.Threading.Timer(_ =>
            {
                var fraction = Math.Min(1.0, stopwatch.Elapsed.TotalSeconds / Math.Max(1, _timeLimitSeconds));
                try
                {
                    progress?.Report(new GenerationProgressReport
                    {
                        Overall = 0.5 + fraction * 0.45,
                        Stage = "CP-SAT",
                        StageProgress = fraction
                    });
                }
                catch
                {
                }
            }, null, 0, 500);

            var status = solver.Solve(model);
            progressTimer.Dispose();
            System.Diagnostics.Debug.WriteLine(
                $"CP-SAT: {status}, objective={(HasSolution(status) ? solver.ObjectiveValue : double.NaN)}, bound={solver.BestObjectiveBound}, idõ={solver.WallTime():0.0}s");

            return BuildSchedule(requirements, placements, status, solver);
        }

        private static bool HasSolution(CpSolverStatus status) =>
            status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible;

        private static Dictionary<(int Id, int Day), IntVar> AddOccupancyTerms(
            CpModel model,
            IntVar zero,
            Dictionary<(int Id, int Day), Dictionary<int, List<BoolVar>>> map,
            int[] periods,
            int gapWeight,
            int startWeight,
            int singleWeight,
            List<LinearExpr> terms)
        {
            var usedByDay = new Dictionary<(int Id, int Day), IntVar>();
            int n = periods.Length;

            foreach (var (key, byPeriod) in map)
            {
                var occ = new IntVar[n];
                for (int i = 0; i < n; i++)
                {
                    if (!byPeriod.TryGetValue(periods[i], out var list))
                    {
                        occ[i] = zero;
                    }
                    else if (list.Count == 1)
                    {
                        occ[i] = list[0];
                    }
                    else
                    {
                        var o = model.NewBoolVar($"occ_{key.Id}_{key.Day}_{periods[i]}");
                        model.AddMaxEquality(o, list);
                        occ[i] = o;
                    }
                }

                var pre = new IntVar[n];
                var suf = new IntVar[n];
                pre[0] = occ[0];
                for (int i = 1; i < n; i++)
                {
                    pre[i] = Max2(model, pre[i - 1], occ[i], $"pre_{key.Id}_{key.Day}_{i}");
                }
                suf[n - 1] = occ[n - 1];
                for (int i = n - 2; i >= 0; i--)
                {
                    suf[i] = Max2(model, suf[i + 1], occ[i], $"suf_{key.Id}_{key.Day}_{i}");
                }

                var used = pre[n - 1];
                usedByDay[key] = used;

                // Gap: üres periódus az elsõ és az utolsó óra között
                if (gapWeight > 0)
                {
                    for (int i = 1; i < n - 1; i++)
                    {
                        var gap = model.NewBoolVar($"gap_{key.Id}_{key.Day}_{i}");
                        model.Add(gap >= pre[i - 1] + suf[i + 1] - occ[i] - 1);
                        model.Add(gap <= pre[i - 1]);
                        model.Add(gap <= suf[i + 1]);
                        model.Add(gap + occ[i] <= 1);
                        terms.Add(gap * gapWeight);
                    }
                }

                // Nap kezdete: (elsõ periódus - 1) * súly
                if (startWeight > 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        var isStart = model.NewBoolVar($"start_{key.Id}_{key.Day}_{i}");
                        if (i == 0)
                        {
                            model.Add(isStart == occ[i]);
                        }
                        else
                        {
                            model.Add(isStart >= occ[i] - pre[i - 1]);
                            model.Add(isStart <= occ[i]);
                            model.Add(isStart + pre[i - 1] <= 1);
                        }

                        var offset = periods[i] - 1;
                        if (offset > 0)
                        {
                            terms.Add(isStart * (offset * startWeight));
                        }
                    }
                }

                // Pontosan egy órás nap büntetése
                if (singleWeight > 0)
                {
                    var lessonCount = LinearExpr.Sum(occ);
                    var single = model.NewBoolVar($"single_{key.Id}_{key.Day}");
                    model.Add(lessonCount == 1).OnlyEnforceIf(single);
                    model.Add(lessonCount != 1).OnlyEnforceIf(single.Not());
                    terms.Add(single * singleWeight);
                }

                // Explicit lineáris kapcsolatok a szorosabb LP-relaxációhoz.
                for (int i = 0; i < n; i++)
                {
                    model.Add(occ[i] <= pre[i]);
                    model.Add(pre[i] <= used);
                    model.Add(occ[i] <= suf[i]);
                    if (i > 0)
                    {
                        model.Add(pre[i - 1] <= pre[i]);
                        model.Add(pre[i] <= pre[i - 1] + occ[i]);
                    }
                    if (i < n - 1)
                    {
                        model.Add(suf[i + 1] <= suf[i]);
                        model.Add(suf[i] <= suf[i + 1] + occ[i]);
                    }
                }
            }

            return usedByDay;
        }

        /// <summary>
        /// DoubleLessonPreference: preferált dupla óránál a párosítatlan órák, egyébként a szomszédos párok büntetettek.
        /// </summary>
        private static void AddDoubleLessonTerms(
            CpModel model,
            LessonRequirement req,
            Dictionary<int, (TimeSlot Slot, BoolVar X)> slotMap,
            List<LinearExpr> terms)
        {
            if (req.PreferDoubleLesson && req.WeeklyHours < 2)
            {
                return;
            }

            foreach (var day in slotMap.Values.GroupBy(s => s.Slot.Day))
            {
                var byPeriod = day.ToDictionary(s => s.Slot.Period, s => s.X);
                var links = new Dictionary<int, BoolVar>();

                foreach (var (period, x) in byPeriod.OrderBy(k => k.Key))
                {
                    if (!byPeriod.TryGetValue(period + 1, out var next))
                    {
                        continue;
                    }

                    var link = model.NewBoolVar($"dl_{req.Id}_{day.Key}_{period}");
                    model.Add(link <= x);
                    model.Add(link <= next);
                    model.Add(link >= x + next - 1);
                    links[period] = link;
                }

                if (req.PreferDoubleLesson)
                {
                    foreach (var (period, link) in links)
                    {
                        if (links.TryGetValue(period + 1, out var nextLink))
                        {
                            model.Add(link + nextLink <= 1);
                        }
                    }

                    var lessons = LinearExpr.Sum(byPeriod.Values);
                    var pairs = LinearExpr.Sum(links.Values) * 2;
                    var unpaired = model.NewIntVar(0, byPeriod.Count, $"unp_{req.Id}_{day.Key}");
                    model.Add(unpaired >= lessons - pairs);
                    terms.Add(unpaired * DoubleLessonPreference.Penalty);
                }
                else if (links.Count > 0)
                {
                    terms.Add(LinearExpr.Sum(links.Values) * DoubleLessonPreference.Penalty);
                }
            }
        }

        private static void AddLessonSplitTerms(
            CpModel model,
            Dictionary<(int ClassId, int SubjectId, int Day), Dictionary<int, List<BoolVar>>> map,
            List<LinearExpr> terms)
        {
            foreach (var (key, byPeriod) in map)
            {
                if (byPeriod.Count < 2)
                {
                    continue;
                }

                var starts = new List<BoolVar>();
                foreach (var period in byPeriod.Keys.OrderBy(p => p))
                {
                    var list = byPeriod[period];
                    var occ = LinearExpr.Sum(list);
                    var start = model.NewBoolVar($"ls_{key.ClassId}_{key.SubjectId}_{key.Day}_{period}");

                    if (byPeriod.TryGetValue(period - 1, out var prev))
                    {
                        var prevOcc = LinearExpr.Sum(prev);
                        model.Add(start >= occ - prevOcc);
                        model.Add(start <= occ);
                        model.Add(start <= 1 - prevOcc);
                    }
                    else
                    {
                        model.Add(start == occ);
                    }
                    starts.Add(start);
                }

                var gaps = model.NewIntVar(0, starts.Count, $"lsgap_{key.ClassId}_{key.SubjectId}_{key.Day}");
                model.Add(gaps >= LinearExpr.Sum(starts) - 1);
                terms.Add(gaps * LessonSplitOnSameDay.PenaltyPerBreak);
            }
        }

        private static void AddParallelGroupSynchronizationTerms(
            CpModel model,
            Dictionary<(string ParallelKey, int SubjectId, int SlotKey), List<BoolVar>> parallelSubjectSlot,
            List<LinearExpr> terms)
        {
            foreach (var band in parallelSubjectSlot.GroupBy(kv => (kv.Key.ParallelKey, kv.Key.SubjectId), StringTupleComparer.OrdinalIgnoreCaseBandSubjectInstance))
            {
                var slotCounts = band
                    .Select(kv =>
                    {
                        var count = model.NewIntVar(0, kv.Value.Count, $"pgc_{Sanitize(kv.Key.ParallelKey)}_{kv.Key.SubjectId}_{kv.Key.SlotKey}");
                        model.Add(count == LinearExpr.Sum(kv.Value));
                        return count;
                    })
                    .ToList();

                if (slotCounts.Count == 0)
                {
                    continue;
                }

                var maxParallel = slotCounts.Max(v => v.Proto.Domain.Last());
                var bestAligned = model.NewIntVar(0, maxParallel, $"pgbest_{Sanitize(band.Key.ParallelKey)}_{band.Key.SubjectId}");
                model.AddMaxEquality(bestAligned, slotCounts);

                var totalLessons = model.NewIntVar(0, slotCounts.Sum(v => (int)v.Proto.Domain.Last()), $"pgtotal_{Sanitize(band.Key.ParallelKey)}_{band.Key.SubjectId}");
                model.Add(totalLessons == LinearExpr.Sum(slotCounts));

                var missing = model.NewIntVar(0, slotCounts.Sum(v => (int)v.Proto.Domain.Last()), $"pgmiss_{Sanitize(band.Key.ParallelKey)}_{band.Key.SubjectId}");
                model.Add(missing == totalLessons - bestAligned);
                terms.Add(missing * ParallelGroupSynchronization.PenaltyPerMissingParallelLesson);
            }
        }

        private static IntVar Max2(CpModel model, IntVar a, IntVar b, string name)
        {
            var v = model.NewBoolVar(name);
            model.AddMaxEquality(v, new[] { a, b });
            return v;
        }

        private Schedule BuildSchedule(
            List<LessonRequirement> requirements,
            List<Placement> placements,
            CpSolverStatus status,
            CpSolver solver)
        {
            var schedule = new Schedule();
            var allLessons = _lessonsGenerator.Generate(requirements);

            if (!HasSolution(status))
            {
                schedule.UnScheduledLessons.AddRange(allLessons);
                return schedule;
            }

            var chosen = placements
                .Where(p => solver.BooleanValue(p.Var))
                .GroupBy(p => p.Requirement)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Slot.Day).ThenBy(p => p.Slot.Period).ToList());

            foreach (var group in allLessons.GroupBy(l => l.Requirement))
            {
                chosen.TryGetValue(group.Key, out var picks);
                picks ??= new List<Placement>();

                int i = 0;
                foreach (var lesson in group)
                {
                    if (i < picks.Count)
                    {
                        lesson.AssignedTimeSlot = picks[i].Slot;
                        lesson.AssignedRoom = picks[i].Room;
                        schedule.AddLesson(lesson);
                        i++;
                    }
                    else
                    {
                        schedule.UnScheduledLessons.Add(lesson);
                    }
                }
            }

            return schedule;
        }

        private static int SlotKey(TimeSlot ts) => ts.Day * 1000 + ts.Period;

        private static IEnumerable<string> ConflictUnits(ClassGroup? cg)
        {
            if (cg == null)
            {
                yield break;
            }

            if (cg is Class schoolClass)
            {
                yield return $"C{schoolClass.Id}";
                yield break;
            }

            if (cg is Group group)
            {
                yield return $"G:{group.Id}";

                foreach (var schoolClassInGroup in group.GetMemberClasses().GroupBy(c => c.Id).Select(g => g.First()))
                {
                    yield return $"C{schoolClassInGroup.Id}";
                }
            }
        }

        private static void AddPeriod<TKey>(
            Dictionary<TKey, Dictionary<int, List<BoolVar>>> map,
            TKey key,
            int period,
            BoolVar x) where TKey : notnull
        {
            if (!map.TryGetValue(key, out var byPeriod))
            {
                map[key] = byPeriod = new Dictionary<int, List<BoolVar>>();
            }
            if (!byPeriod.TryGetValue(period, out var list))
            {
                byPeriod[period] = list = new List<BoolVar>();
            }
            list.Add(x);
        }

        private static void Add<TKey>(Dictionary<TKey, List<BoolVar>> map, TKey key, BoolVar v) where TKey : notnull
        {
            if (!map.TryGetValue(key, out var list))
            {
                map[key] = list = new List<BoolVar>();
            }
            list.Add(v);
        }

        private static void AddAtMostOne(CpModel model, IEnumerable<List<BoolVar>> groups)
        {
            foreach (var list in groups)
            {
                if (list.Count > 1)
                {
                    model.AddAtMostOne(list);
                }
            }
        }

        private static void AddClassDayBalanceTerms(
            CpModel model,
            IntVar zero,
            Dictionary<(int Id, int Day), Dictionary<int, List<BoolVar>>> classDay,
            List<LinearExpr> terms)
        {
            foreach (var byClass in classDay.GroupBy(kv => kv.Key.Id))
            {
                var counts = new LinearExpr[ClassDayBalance.DaysPerWeek];
                int total = 0;

                for (int d = 1; d <= ClassDayBalance.DaysPerWeek; d++)
                {
                    if (classDay.TryGetValue((byClass.Key, d), out var byPeriod))
                    {
                        var vars = byPeriod.Values.SelectMany(v => v).ToList();
                        total += vars.Count;
                        counts[d - 1] = vars.Count == 0 ? zero : LinearExpr.Sum(vars);
                    }
                    else
                    {
                        counts[d - 1] = zero;
                    }
                }

                var maxCount = model.NewIntVar(0, total, $"maxday_{byClass.Key}");
                var minCount = model.NewIntVar(0, total, $"minday_{byClass.Key}");
                foreach (var count in counts)
                {
                    model.Add(maxCount >= count);
                    model.Add(minCount <= count);
                }

                var spread = model.NewIntVar(0, total, $"spread_{byClass.Key}");
                model.Add(spread == maxCount - minCount);
                terms.Add(spread * ClassDayBalance.PenaltyPerLessonDifference);
            }
        }

        private static string Sanitize(string value)
        {
            return string.Concat(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
        }

        private sealed class StringTupleComparer : IEqualityComparer<(string ParallelKey, int SubjectId, int SlotKey)>, IEqualityComparer<(string ParallelKey, int SubjectId)>
        {
            public static readonly StringTupleComparer OrdinalIgnoreCaseInstance = new();
            public static readonly StringTupleComparer OrdinalIgnoreCaseBandSubjectInstance = new();

            public bool Equals((string ParallelKey, int SubjectId, int SlotKey) x, (string ParallelKey, int SubjectId, int SlotKey) y)
                => string.Equals(x.ParallelKey, y.ParallelKey, StringComparison.OrdinalIgnoreCase)
                   && x.SubjectId == y.SubjectId
                   && x.SlotKey == y.SlotKey;

            public int GetHashCode((string ParallelKey, int SubjectId, int SlotKey) obj)
                => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ParallelKey), obj.SubjectId, obj.SlotKey);

            public bool Equals((string ParallelKey, int SubjectId) x, (string ParallelKey, int SubjectId) y)
                => string.Equals(x.ParallelKey, y.ParallelKey, StringComparison.OrdinalIgnoreCase)
                   && x.SubjectId == y.SubjectId;

            public int GetHashCode((string ParallelKey, int SubjectId) obj)
                => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ParallelKey), obj.SubjectId);
        }

        private sealed record Placement(LessonRequirement Requirement, TimeSlot Slot, Room Room, BoolVar Var);
    }
}