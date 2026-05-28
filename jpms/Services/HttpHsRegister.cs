using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Hs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpHsRegister : IHsRegister
{
    private readonly HsRecordsReadModel readModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpHsRegister(HsRecordsReadModel readModel, IQueryClient queries, ICommandSender commands)
    {
        this.readModel = readModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<HsRecord> All()
    {
        if (readModel.Current is null) _ = readModel.RefreshAsync(CancellationToken.None);
        return readModel.Current ?? Array.Empty<HsRecord>();
    }

    public IReadOnlyList<HsRecord> ByKind(HsRecordKind kind) =>
        All().Where(record => record.Kind == kind).ToList().AsReadOnly();

    public IReadOnlyList<HsRecord> ForProject(string projectId) =>
        All().Where(record => string.Equals(record.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();

    public HsRecord Upsert(HsRecord record)
    {
        if (string.IsNullOrEmpty(record.HsRecordId))
            _ = LogAsync(record);
        else _ = UpdateAsync(record);
        return record;
    }

    public IReadOnlyList<HsRecordAttendance> AttendanceFor(string hsRecordId) =>
        queries.AskAsync(new ListAttendanceForHsRecord(hsRecordId), CancellationToken.None).GetAwaiter().GetResult();

    public void SaveAttendance(HsRecordAttendance attendance) =>
        _ = commands.SendAsync(new RecordAttendanceForHsRecord(attendance.HsRecordId, attendance.AttendeeName, attendance.SignatureBlobRef), CancellationToken.None);

    private async Task LogAsync(HsRecord record)
    {
        await commands.SendAsync(new LogHsRecord(record.ProjectId, record.Kind, record.Summary, record.Severity, record.AssignedToEmail, record.DueAt), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    private async Task UpdateAsync(HsRecord record)
    {
        await commands.SendAsync(new UpdateHsRecord(record.HsRecordId, record.Summary, record.Severity, record.Status, record.AssignedToEmail, record.DueAt), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }
}
