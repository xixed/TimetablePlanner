using System.Text.Json;
using TimetablePlanner.Core.Models;
using TimetablePlanner.Data.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;


namespace TimetablePlanner.Data.Seed
{
    public static class JsonImporter
    {
        public static void ImportFromFile(TimetableDbContext context, string jsonFilePath, bool clearExisting = false)
        {
            if (!File.Exists(jsonFilePath)) throw new FileNotFoundException(jsonFilePath);

            var json = File.ReadAllText(jsonFilePath);
            var dto = JsonSerializer.Deserialize<TestSchoolDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? throw new InvalidOperationException("Failed to parse JSON.");

            using var tx = context.Database.BeginTransaction();
            try
            {
                if (clearExisting)
                {
                    // adjust to your cascade rules / table names
                    context.LessonRequirements.RemoveRange(context.LessonRequirements);
                    context.TimeSlots.RemoveRange(context.TimeSlots);
                    context.Rooms.RemoveRange(context.Rooms);
                    context.Classes.RemoveRange(context.Classes);
                    context.Groups.RemoveRange(context.Groups);
                    context.Teachers.RemoveRange(context.Teachers);
                    context.TeacherTypes.RemoveRange(context.TeacherTypes);
                    context.Subjects.RemoveRange(context.Subjects);
                    context.SaveChanges();
                }

                // Subjects
                var subjects = new List<Subject>();
                var subjectById = new Dictionary<int, Subject>();
                if (dto.Subjects != null)
                {
                    foreach (var sDto in dto.Subjects)
                    {
                        var s = new Subject { Name = string.IsNullOrWhiteSpace(sDto.Name) ? $"Subject_{sDto.Id}" : sDto.Name! };
                        subjects.Add(s);
                        subjectById[sDto.Id] = s;
                    }
                }
                context.Subjects.AddRange(subjects);

                // TeacherTypes
                var types = new List<TeacherType>();
                var typeById = new Dictionary<int, TeacherType>();
                if (dto.TeacherTypes != null)
                {
                    foreach (var tDto in dto.TeacherTypes)
                    {
                        var t = new TeacherType { RequiredWeeklyHours = tDto.RequiredWeeklyHours, MaxWeeklyHours = tDto.MaxWeeklyHours };
                        types.Add(t);
                        typeById[tDto.Id] = t;
                    }
                }
                context.TeacherTypes.AddRange(types);

                // Teachers (assign Subjects and TeacherType)
                var teachers = new List<Teacher>();
                var teacherById = new Dictionary<int, Teacher>();
                if (dto.Teachers != null)
                {
                    foreach (var tDto in dto.Teachers)
                    {
                        var teacher = new Teacher
                        {
                            Name = string.IsNullOrWhiteSpace(tDto.Name) ? $"Teacher_{tDto.Id}" : tDto.Name!,
                            TeacherType = (tDto.TeacherTypeId.HasValue && typeById.TryGetValue(tDto.TeacherTypeId.Value, out var tt)) ? tt : null,
                            Subjects = new List<Subject>(),
                            UnavailableTimeSlots = new List<TimeSlot>()
                        };

                        if (tDto.SubjectIds != null)
                        {
                            foreach (var sid in tDto.SubjectIds)
                                if (subjectById.TryGetValue(sid, out var subj)) teacher.Subjects.Add(subj);
                        }

                        teachers.Add(teacher);
                        teacherById[tDto.Id] = teacher;
                    }
                }
                context.Teachers.AddRange(teachers);

                // Classes
                var classes = new List<Class>();
                var classById = new Dictionary<int, Class>();
                if (dto.Classes != null)
                {
                    foreach (var cDto in dto.Classes)
                    {
                        var c = new Class
                        {
                            Name = string.IsNullOrWhiteSpace(cDto.Name) ? $"Class_{cDto.Id}" : cDto.Name!,
                            Size = cDto.Size,
                            Subjects = new List<Subject>()
                        };
                        classes.Add(c);
                        classById[cDto.Id] = c;
                    }
                }
                context.Classes.AddRange(classes);

                // Groups (import and wire Classes if present)
                var groups = new List<Group>();
                var groupById = new Dictionary<int, Group>();
                if (dto.Groups != null)
                {
                    foreach (var gDto in dto.Groups)
                    {
                        var g = new Group
                        {
                            Name = string.IsNullOrWhiteSpace(gDto.Name) ? $"Group_{gDto.Id}" : gDto.Name!,
                            Size = gDto.Size,
                            Subjects = new List<Subject>(),
                            Classes = new List<Class>()
                        };

                        if (gDto.ClassIds != null)
                        {
                            foreach (var cid in gDto.ClassIds)
                                if (classById.TryGetValue(cid, out var cls)) g.Classes.Add(cls);
                        }

                        groups.Add(g);
                        groupById[gDto.Id] = g;
                    }
                }
                if (groups.Any()) context.Groups.AddRange(groups);

                // Build combined ClassGroup dictionary so LessonRequirements can reference either Class or Group
                var classGroupById = new Dictionary<int, ClassGroup>();
                foreach (var kv in classById) classGroupById[kv.Key] = kv.Value;
                foreach (var kv in groupById) classGroupById[kv.Key] = kv.Value;

                foreach (var kv in classById)
                {
                    if (string.IsNullOrWhiteSpace(kv.Value.Name))
                        kv.Value.Name = $"Class_{kv.Key}";
                }
                foreach (var kv in groupById)
                {
                    if (string.IsNullOrWhiteSpace(kv.Value.Name))
                        kv.Value.Name = $"Group_{kv.Key}";
                }

                // Rooms
                var rooms = new List<Room>();
                var roomById = new Dictionary<int, Room>();
                if (dto.Rooms != null)
                {
                    foreach (var rDto in dto.Rooms)
                    {
                        var r = new Room { Name = string.IsNullOrWhiteSpace(rDto.Name) ? $"Room_{rDto.Id}" : rDto.Name!, Capacity = rDto.Capacity, Subjects = new List<Subject>() };
                        rooms.Add(r);
                        roomById[rDto.Id] = r;
                    }
                }
                context.Rooms.AddRange(rooms);

                // TimeSlots
                var times = new List<TimeSlot>();
                var timeById = new Dictionary<int, TimeSlot>();
                if (dto.TimeSlots != null)
                {
                    foreach (var tDto in dto.TimeSlots)
                    {
                        var ts = new TimeSlot { Day = tDto.Day, Period = tDto.Period };
                        times.Add(ts);
                        timeById[tDto.Id] = ts;
                    }
                }
                context.TimeSlots.AddRange(times);

                context.SaveChanges(); // persist basics so relationships can reference tracked entities

                // LessonRequirements (wire navigation props using dictionaries)
                var requirements = new List<LessonRequirement>();
                foreach (var ld in dto.LessonRequirements ?? Enumerable.Empty<LessonReqDto>())
                {
                    var req = new LessonRequirement
                    {
                        WeeklyHours = ld.WeeklyHours,
                        Subject = subjectById.TryGetValue(ld.SubjectId, out var s) ? s : null,
                        Teacher = teacherById.TryGetValue(ld.TeacherId, out var tchr) ? tchr : null,
                        ClassGroup = classGroupById.TryGetValue(ld.ClassGroupId, out var cg) ? cg : null,
                        SuitableRooms = new List<Room>(),
                        PossibleTimeSlots = new List<TimeSlot>()
                    };

                    if (ld.SuitableRoomIds != null)
                    {
                        foreach (var rid in ld.SuitableRoomIds)
                            if (roomById.TryGetValue(rid, out var r)) req.SuitableRooms.Add(r);
                    }

                    if (ld.PossibleTimeSlotIds != null)
                    {
                        foreach (var tid in ld.PossibleTimeSlotIds)
                            if (timeById.TryGetValue(tid, out var ts)) req.PossibleTimeSlots.Add(ts);
                    }

                    // validate required non-null fields that your DB expects
                    if (req.ClassGroup == null) throw new InvalidOperationException($"LessonRequirement {ld.Id} references missing classGroup {ld.ClassGroupId}");
                    if (req.Subject == null) throw new InvalidOperationException($"LessonRequirement {ld.Id} references missing subject {ld.SubjectId}");
                    if (req.Teacher == null) throw new InvalidOperationException($"LessonRequirement {ld.Id} references missing teacher {ld.TeacherId}");

                    // Ensure ClassGroup.Name satisfies NOT NULL DB constraint
                    if (string.IsNullOrWhiteSpace(req.ClassGroup.Name))
                        req.ClassGroup.Name = $"ClassGroup_{ld.ClassGroupId}";

                    requirements.Add(req);
                }

                context.LessonRequirements.AddRange(requirements);
                context.SaveChanges();
                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { /* ignore rollback errors */ }
                throw;
            }
        }

        // DTOs mirroring the JSON structure (only necessary fields)
        private class TestSchoolDto
        {
            public List<SubjectDto>? Subjects { get; set; }
            public List<TeacherTypeDto>? TeacherTypes { get; set; }
            public List<TeacherDto>? Teachers { get; set; }
            public List<ClassDto>? Classes { get; set; }
            public List<GroupDto>? Groups { get; set; }
            public List<RoomDto>? Rooms { get; set; }
            public List<TimeSlotDto>? TimeSlots { get; set; }
            public List<LessonReqDto>? LessonRequirements { get; set; }
        }

        private class SubjectDto { public int Id { get; set; } public string? Name { get; set; } }
        private class TeacherTypeDto { public int Id { get; set; } public int RequiredWeeklyHours { get; set; } public int MaxWeeklyHours { get; set; } }
        private class TeacherDto { public int Id { get; set; } public string? Name { get; set; } public int? TeacherTypeId { get; set; } public List<int>? SubjectIds { get; set; } }
        private class ClassDto { public int Id { get; set; } public string? Name { get; set; } public int Size { get; set; } }
        private class GroupDto { public int Id { get; set; } public string? Name { get; set; } public int Size { get; set; } public List<int>? ClassIds { get; set; } }
        private class RoomDto { public int Id { get; set; } public string? Name { get; set; } public int Capacity { get; set; } }
        private class TimeSlotDto { public int Id { get; set; } public int Day { get; set; } public int Period { get; set; } }
        private class LessonReqDto { public int Id { get; set; } public int SubjectId { get; set; } public int TeacherId { get; set; } public int ClassGroupId { get; set; } public int WeeklyHours { get; set; } public List<int>? SuitableRoomIds { get; set; } public List<int>? PossibleTimeSlotIds { get; set; } }
    }
}