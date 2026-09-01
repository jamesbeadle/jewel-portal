using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Features.Calendar;

public sealed class CalendarReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<CalendarEvent>> eventsByProject = new();

    public CalendarReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<CalendarEvent> Current(string projectId) =>
        eventsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<CalendarEvent>();
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => eventsByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        eventsByProject[projectId] = await queries.AskAsync(new ListCalendarEventsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
