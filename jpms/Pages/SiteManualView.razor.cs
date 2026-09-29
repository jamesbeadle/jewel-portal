using Jewel.JPMS.Features.Manual;

namespace Jewel.JPMS.Pages;

/// <summary>A role view of the manual, re-read after every acknowledgement so the pills tell the truth.</summary>
public partial class SiteManualView
{
    private const string PrintFunction = "window.print";

    [Inject] private SessionService Session { get; set; } = default!;
    [Inject] private AuthService Auth { get; set; } = default!;
    [Inject] private IQueryClient Queries { get; set; } = default!;

    [Parameter] public string View { get; set; } = "";

    private ManualView view = ManualView.SiteManager;
    private ManualViewReading? reading;
    private string? note;

    private bool CanAcknowledge => Session.ActiveRole is { } role && ManualRoles.AllowedToReadViews.Includes(role);

    private string SuggestedName
    {
        get
        {
            var user = Auth.CurrentUser;
            return user?.DisplayName ?? "";
        }
    }

    private string IssueLine => reading is null
        ? ""
        : $"Issued {DateTimeText(reading.IssuedAt)} · {reading.ModuleCount} approved modules";

    protected override async Task OnParametersSetAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Enum.TryParse(View, ignoreCase: true, out view)) { note = $"There is no view called {View}."; return; }
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try { reading = await Queries.AskAsync(new GetManualView(view), CancellationToken.None); }
        catch (Exception) { note = "Couldn't load the manual — reload to try again."; }
    }

    private Task PrintAsync() => JavaScript.InvokeVoidAsync(PrintFunction).AsTask();
}
