using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryChangeRegister : IChangeRegister
{
    private const string NigelEmail = "Nigel.Reilly@jewelenterprises.co.uk";

    private readonly List<ChangeRecord> records = new()
    {
        new("CR-001", "PRJ-001", ChangeKind.Rfi,           "RFI-001", "Slate roof flashing detail",       "Need detail clarification at gable to chimney junction.",      ChangeStatus.AwaitingResponse, null,      NigelEmail, DateTimeOffset.UtcNow.AddDays(-7),  null),
        new("CR-002", "PRJ-001", ChangeKind.Variation,    "VO-001",  "Upgrade staircase oak grade",       "Client request — premium oak in lieu of select.",              ChangeStatus.Approved,        +6_500m,   NigelEmail, DateTimeOffset.UtcNow.AddDays(-14), DateTimeOffset.UtcNow.AddDays(-8)),
        new("CR-003", "PRJ-001", ChangeKind.Submittal,    "SUB-001", "Brick sample for approval",         "Architect to confirm facing brick selection.",                 ChangeStatus.Approved,        null,      NigelEmail, DateTimeOffset.UtcNow.AddDays(-25), DateTimeOffset.UtcNow.AddDays(-22)),
        new("CR-004", "PRJ-001", ChangeKind.NoticeOfDelay,"NOD-001", "Late drawings — north elevation",    "5 working day delay to elevation works pending revision C.",   ChangeStatus.Closed,          null,      NigelEmail, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-25)),
        new("CR-005", "PRJ-002", ChangeKind.Rfi,           "RFI-002", "Foundation tie-in detail",          "Existing wall tie-in approach to confirm.",                    ChangeStatus.Open,            null,      NigelEmail, DateTimeOffset.UtcNow.AddDays(-2),  null)
    };

    public event Action? OnChange;

    public IReadOnlyList<ChangeRecord> ForProject(string projectId) =>
        records.Where(r => string.Equals(r.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
               .OrderByDescending(r => r.RaisedAt).ToList().AsReadOnly();

    public IReadOnlyList<ChangeRecord> ForProject(string projectId, ChangeKind kind) =>
        records.Where(r =>
            string.Equals(r.ProjectId, projectId, StringComparison.OrdinalIgnoreCase) && r.Kind == kind)
               .OrderByDescending(r => r.RaisedAt).ToList().AsReadOnly();

    public ChangeRecord? Find(string changeRecordId) =>
        records.FirstOrDefault(r =>
            string.Equals(r.ChangeRecordId, changeRecordId, StringComparison.OrdinalIgnoreCase));

    public ChangeRecord Upsert(ChangeRecord record)
    {
        var existing = Find(record.ChangeRecordId);
        if (existing is not null) records.Remove(existing);
        records.Add(record);
        OnChange?.Invoke();
        return record;
    }
}
