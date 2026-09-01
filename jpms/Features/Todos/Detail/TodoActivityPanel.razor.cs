
namespace Jewel.JPMS.Features.Todos.Detail;

// The timeline panel's working state: its own read (gated on its own load), the log-a-chase form,
// and a Version the page bumps after any command so the newest line appears without a reload.
public partial class TodoActivityPanel
{
    [Parameter, EditorRequired] public string TodoItemId { get; set; } = "";
    /// <summary>Bumped by the page after every item-changing command; a change re-reads the rows.</summary>
    [Parameter] public int Version { get; set; }
    /// <summary>Whether the reader may log progress (manage gate, or the item is theirs).</summary>
    [Parameter] public bool CanLog { get; set; }
    [Parameter] public bool Busy { get; set; }
    /// <summary>The page runs the command and answers whether it landed.</summary>
    [Parameter] public Func<TodoActivityKind, string?, Task<bool>> LogProgress { get; set; } = (_, _) => Task.FromResult(false);

    private bool loading = true;
    private bool failed;
    private bool formOpen;
    private int loadedVersion = -1;
    private IReadOnlyList<TodoActivity> rows = Array.Empty<TodoActivity>();

    protected override async Task OnParametersSetAsync()
    {
        if (loadedVersion == Version) return;
        loadedVersion = Version;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        failed = false;
        try { rows = await Todos.ListActivityAsync(TodoItemId); }
        catch { failed = true; }
        finally { loading = false; }
    }

    // The page's command writes the line; the panel re-reads so the story shows it at once.
    private async Task<bool> SaveAsync(TodoActivityKind kind, string? note)
    {
        var saved = await LogProgress(kind, note);
        if (saved) await LoadAsync();
        return saved;
    }
}
