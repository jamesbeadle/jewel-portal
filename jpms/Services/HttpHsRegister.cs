using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpHsRegister : IHsRegister
{
    private readonly HttpClient httpClient;
    private IReadOnlyList<HsRecord> cached = Array.Empty<HsRecord>();
    private bool hasLoaded;
    private readonly Dictionary<string, IReadOnlyList<HsRecordAttendance>> attendanceByRecord = new();

    public HttpHsRegister(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<HsRecord> All()
    {
        if (!hasLoaded) _ = LoadAsync();
        return cached;
    }

    public IReadOnlyList<HsRecord> ByKind(HsRecordKind kind) =>
        All().Where(r => r.Kind == kind).ToList().AsReadOnly();

    public IReadOnlyList<HsRecord> ForProject(string projectId) =>
        All().Where(r => string.Equals(r.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();

    public HsRecord Upsert(HsRecord record)
    {
        _ = PostRecordAsync(record);
        return record;
    }

    public IReadOnlyList<HsRecordAttendance> AttendanceFor(string hsRecordId)
    {
        if (!attendanceByRecord.ContainsKey(hsRecordId)) _ = LoadAttendanceAsync(hsRecordId);
        return attendanceByRecord.TryGetValue(hsRecordId, out var list) ? list : Array.Empty<HsRecordAttendance>();
    }

    public void SaveAttendance(HsRecordAttendance attendance) =>
        _ = SaveAttendanceAsync(attendance);

    private async Task LoadAsync()
    {
        hasLoaded = true;
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<HsRecord>>("/api/hs-records");
            cached = response?.AsReadOnly() ?? (IReadOnlyList<HsRecord>)Array.Empty<HsRecord>();
            OnChange?.Invoke();
        }
        catch { cached = Array.Empty<HsRecord>(); }
    }

    private async Task LoadAttendanceAsync(string hsRecordId)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<HsRecordAttendance>>($"/api/hs-records/{hsRecordId}/attendance");
            attendanceByRecord[hsRecordId] = response?.AsReadOnly() ?? (IReadOnlyList<HsRecordAttendance>)Array.Empty<HsRecordAttendance>();
            OnChange?.Invoke();
        }
        catch { attendanceByRecord[hsRecordId] = Array.Empty<HsRecordAttendance>(); }
    }

    private async Task PostRecordAsync(HsRecord record)
    {
        try { await httpClient.PostAsJsonAsync("/api/hs-records", record); } catch { return; }
        await LoadAsync();
    }

    private async Task SaveAttendanceAsync(HsRecordAttendance attendance)
    {
        try { await httpClient.PostAsJsonAsync("/api/hs-records/attendance", attendance); } catch { return; }
        attendanceByRecord.Remove(attendance.HsRecordId);
        await LoadAttendanceAsync(attendance.HsRecordId);
    }
}
