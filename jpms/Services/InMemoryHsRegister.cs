using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryHsRegister : IHsRegister
{
    private const string NigelEmail = "Nigel.Reilly@jewelenterprises.co.uk";

    private readonly List<HsRecord> records = new()
    {
        new("HS-001", "PRJ-001", HsRecordKind.Observation,      "Loose scaffold board near north elevation", HsSeverity.Medium, HsStatus.Closed,     NigelEmail, DateTimeOffset.UtcNow.AddDays(-21), null, DateTimeOffset.UtcNow.AddDays(-20)),
        new("HS-002", "PRJ-001", HsRecordKind.NearMiss,         "Tool drop from level 1",                    HsSeverity.High,   HsStatus.Closed,     NigelEmail, DateTimeOffset.UtcNow.AddDays(-14), null, DateTimeOffset.UtcNow.AddDays(-12)),
        new("HS-003", "PRJ-001", HsRecordKind.CorrectiveAction, "Install permanent edge protection",         HsSeverity.High,   HsStatus.InProgress, NigelEmail, DateTimeOffset.UtcNow.AddDays(-12), DateTimeOffset.UtcNow.AddDays(3),  null),
        new("HS-004", "PRJ-001", HsRecordKind.ToolboxTalk,      "Working at height refresher",               HsSeverity.Low,    HsStatus.Closed,     NigelEmail, DateTimeOffset.UtcNow.AddDays(-7),  null, DateTimeOffset.UtcNow.AddDays(-7)),
        new("HS-005", "PRJ-002", HsRecordKind.Observation,      "Skip overflowing — review collection",      HsSeverity.Low,    HsStatus.Open,       NigelEmail, DateTimeOffset.UtcNow.AddDays(-2),  DateTimeOffset.UtcNow.AddDays(2),  null),
        new("HS-006", "PRJ-002", HsRecordKind.Permit,           "Hot works in basement plant room",          HsSeverity.High,   HsStatus.Open,       NigelEmail, DateTimeOffset.UtcNow.AddDays(-1),  DateTimeOffset.UtcNow.AddDays(7),  null)
    };

    public event Action? OnChange;

    public IReadOnlyList<HsRecord> All() =>
        records.OrderByDescending(r => r.RaisedAt).ToList().AsReadOnly();

    public IReadOnlyList<HsRecord> ByKind(HsRecordKind kind) =>
        records.Where(r => r.Kind == kind)
               .OrderByDescending(r => r.RaisedAt)
               .ToList()
               .AsReadOnly();

    public IReadOnlyList<HsRecord> ForProject(string projectId) =>
        records.Where(r => string.Equals(r.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
               .OrderByDescending(r => r.RaisedAt)
               .ToList()
               .AsReadOnly();

    public HsRecord Upsert(HsRecord record)
    {
        var existing = records.FirstOrDefault(r => r.HsRecordId == record.HsRecordId);
        if (existing is not null) records.Remove(existing);
        records.Add(record);
        OnChange?.Invoke();
        return record;
    }

    private readonly List<HsRecordAttendance> attendance = new()
    {
        new("AT-001", "HS-004", "Pete Maynard",  "sig-pete.png",  DateTimeOffset.UtcNow.AddDays(-7)),
        new("AT-002", "HS-004", "Linda Hampton", "sig-linda.png", DateTimeOffset.UtcNow.AddDays(-7)),
        new("AT-003", "HS-004", "Raj Patel",     "sig-raj.png",   DateTimeOffset.UtcNow.AddDays(-7))
    };

    public IReadOnlyList<HsRecordAttendance> AttendanceFor(string hsRecordId) =>
        attendance.Where(a => a.HsRecordId == hsRecordId).ToList().AsReadOnly();

    public void SaveAttendance(HsRecordAttendance entry)
    {
        var existing = attendance.FirstOrDefault(a => a.HsRecordAttendanceId == entry.HsRecordAttendanceId);
        if (existing is not null) attendance.Remove(existing);
        attendance.Add(entry);
        OnChange?.Invoke();
    }
}
