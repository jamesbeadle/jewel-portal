using Jewel.JPMS.Contracts.Calendar;
using Jewel.JPMS.Features.Calendar;

namespace Jewel.JPMS.Pages;

public partial class ProjectCalendar
{
    [Parameter] public string ProjectId { get; set; } = "";

    // Deep link from record chips/hrefs elsewhere: /projects/{id}/calendar?event={eventId} opens
    // that event's dialog and jumps the grid to its month.
    [SupplyParameterFromQuery(Name = "event")] public string? FocusEventId { get; set; }

    private static readonly IReadOnlyList<CalendarEvent> NoEvents = Array.Empty<CalendarEvent>();

    private bool sessionReady;
    private bool dataFailed;
    private bool busy;
    private string? actionError;

    private int year;
    private int month;
    private DateOnly? expandedDay;

    // The add/edit dialog's fields — dates as "yyyy-MM-dd" and time as "HH:mm", the inputs' own text.
    private bool editOpen;
    private string? editingId;
    private string editingReference = "";
    private bool confirmingDelete;
    private string? modalError;
    private string fTitle = "";
    private CalendarEventKind fKind = CalendarEventKind.Meeting;
    private string fDate = "";
    private string fStartTime = "";
    private string fEndDate = "";
    private string fNotes = "";
    private bool fClientVisible;
    private IReadOnlyList<MailboxMessage>? linkedEmails;
    private string? focusHandledFor;

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    private string MonthLabel => new DateTime(year, month, 1).ToString("MMMM yyyy");

    private Dictionary<DateOnly, List<CalendarEvent>> EventsByDay =>
        CalendarMaths.ByDay(Calendar.Current(ProjectId));

    private IReadOnlyList<CalendarEvent> Upcoming =>
        CalendarMaths.UpcomingFrom(Calendar.Current(ProjectId), Today);

    // Agenda rows group under the event's start day — or under today for a multi-day event
    // already running, so "what's happening today" includes the visit that began yesterday.
    private DateOnly AgendaDay(CalendarEvent item)
    {
        var start = DateOnly.FromDateTime(item.Date.UtcDateTime);
        return start < Today ? Today : start;
    }

    protected override async Task OnInitializedAsync()
    {
        year = DateTime.Today.Year;
        month = DateTime.Today.Month;
        Calendar.OnChanged += StateHasChanged;
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        sessionReady = true;
        await LoadAsync();
        OpenFocusedEvent();
    }

    private string loadedForProjectId = "";

    protected override async Task OnParametersSetAsync()
    {
        if (!sessionReady) return;
        if (loadedForProjectId != ProjectId)
        {
            // Project switch via the shell's arrows: same page, new key — load the new calendar
            // (and give a previously failed project a fresh chance).
            dataFailed = false;
            expandedDay = null;
            actionError = null;
            CloseEditState();
            await LoadAsync();
        }
        OpenFocusedEvent();
    }

    private async Task LoadAsync()
    {
        loadedForProjectId = ProjectId;
        try { await Calendar.RefreshAsync(ProjectId, CancellationToken.None); }
        catch { dataFailed = true; } // the query client has already toasted the detail
    }

    private void OpenFocusedEvent()
    {
        if (FocusEventId is not { Length: > 0 } targetId || focusHandledFor == targetId) return;
        if (!Calendar.LoadedFor(ProjectId)) return;
        focusHandledFor = targetId;
        var target = Calendar.Current(ProjectId).FirstOrDefault(e => e.CalendarEventId == targetId);
        if (target is null) return;
        year = target.Date.Year;
        month = target.Date.Month;
        OpenEdit(target);
    }

    private void OnKindChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var value)) fKind = (CalendarEventKind)value;
    }

    private void MoveMonth(int delta)
    {
        var moved = new DateTime(year, month, 1).AddMonths(delta);
        year = moved.Year;
        month = moved.Month;
        expandedDay = null;
    }

    private void GoToday()
    {
        year = DateTime.Today.Year;
        month = DateTime.Today.Month;
        expandedDay = null;
    }

    private void OpenAdd()
    {
        CloseEditState();
        fDate = Today.ToString("yyyy-MM-dd");
        editOpen = true;
    }

    private void OpenEdit(CalendarEvent item)
    {
        CloseEditState();
        editingId = item.CalendarEventId;
        editingReference = item.Reference;
        fTitle = item.Title;
        fKind = item.Kind;
        fDate = item.Date.ToString("yyyy-MM-dd");
        fStartTime = item.StartTime ?? "";
        fEndDate = item.EndDate?.ToString("yyyy-MM-dd") ?? "";
        fNotes = item.Notes;
        fClientVisible = item.ClientVisible;
        editOpen = true;
        _ = LoadLinkedEmailsAsync(item.CalendarEventId);
    }

    private async Task LoadLinkedEmailsAsync(string calendarEventId)
    {
        try
        {
            var emails = await Queries.AskAsync(new ListRecordEmails(RecordType.CalendarEvent, calendarEventId), CancellationToken.None);
            if (editingId == calendarEventId) { linkedEmails = emails; StateHasChanged(); }
        }
        catch
        {
            // The toast has the detail; an empty list is the honest fallback for this visit.
            if (editingId == calendarEventId) { linkedEmails = Array.Empty<MailboxMessage>(); StateHasChanged(); }
        }
    }

    private void CloseEdit() => CloseEditState();

    private void CloseEditState()
    {
        editOpen = false;
        editingId = null;
        editingReference = "";
        confirmingDelete = false;
        modalError = null;
        linkedEmails = null;
        fTitle = ""; fKind = CalendarEventKind.Meeting; fDate = ""; fStartTime = ""; fEndDate = ""; fNotes = "";
        fClientVisible = false;
    }

    private CalendarEventDetails? BuildDetails()
    {
        if (!DateTime.TryParseExact(fDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            modalError = "A date is required.";
            return null;
        }
        DateTimeOffset? endDate = null;
        if (!string.IsNullOrWhiteSpace(fEndDate))
        {
            if (!DateTime.TryParseExact(fEndDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var end))
            {
                modalError = "The end date isn't a date.";
                return null;
            }
            if (end < date)
            {
                modalError = "End date can't be before the start date.";
                return null;
            }
            if (end != date) endDate = new DateTimeOffset(end, TimeSpan.Zero);
        }
        return new CalendarEventDetails(
            fTitle.Trim(),
            fKind,
            new DateTimeOffset(date, TimeSpan.Zero),
            string.IsNullOrWhiteSpace(fStartTime) ? null : fStartTime,
            endDate,
            fNotes.Trim(),
            fClientVisible);
    }

    private async Task SaveAsync()
    {
        modalError = null;
        var details = BuildDetails();
        if (details is null) return;
        busy = true;
        try
        {
            if (editingId is null)
                await Commands.SendAsync(new CreateCalendarEvent(ProjectId, details), CancellationToken.None);
            else
                await Commands.SendAsync(new UpdateCalendarEvent(editingId, details), CancellationToken.None);
            CloseEditState();
            await RefreshAfterWriteAsync();
        }
        catch (CommandFailedException ex) { modalError = ex.Message; }
        finally { busy = false; }
    }

    private async Task DeleteAsync()
    {
        if (editingId is null) return;
        if (!confirmingDelete) { confirmingDelete = true; return; }
        busy = true;
        try
        {
            await Commands.SendAsync(new DeleteCalendarEvent(editingId), CancellationToken.None);
            CloseEditState();
            await RefreshAfterWriteAsync();
        }
        catch (CommandFailedException ex) { modalError = ex.Message; confirmingDelete = false; }
        finally { busy = false; }
    }

    // Post-write reload: swallow query failures (the toast already reported them) so a 502 on the
    // refetch can't take the page down after a successful write — see post-write-reload rule.
    private async Task RefreshAfterWriteAsync()
    {
        try { await Calendar.RefreshAsync(ProjectId, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private static string ChipTitle(CalendarEvent item)
    {
        var time = item.StartTime is { } start ? $" {start}" : "";
        var span = item.IsMultiDay ? $" (to {item.LastDate:d MMM})" : "";
        return $"{item.Reference} · {CalendarEventKinds.Label(item.Kind)}{time}{span} — {item.Title}";
    }

    private static string KindDot(CalendarEventKind kind) => kind switch
    {
        CalendarEventKind.SiteVisit => "bg-info",
        CalendarEventKind.Delivery => "bg-warning",
        CalendarEventKind.Meeting => "bg-accent",
        CalendarEventKind.SubcontractorAttendance => "bg-positive",
        _ => "bg-content-subtle"
    };

    public void Dispose() => Calendar.OnChanged -= StateHasChanged;
}
