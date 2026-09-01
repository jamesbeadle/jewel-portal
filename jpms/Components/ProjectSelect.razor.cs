
namespace Jewel.JPMS.Components;

public partial class ProjectSelect : IDisposable
{
    /// <summary>Every project the picker offers — pass the read model's list raw; the component
    /// orders it itself (InWorkOrder) and splits completed projects behind the toggle.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();

    /// <summary>The picked project id ("" = none). A completed project already picked keeps its
    /// label on the toggle whatever the completed filter says.</summary>
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    [Parameter] public string Placeholder { get; set; } = "No project…";

    /// <summary>Offer a clear row at the top — off for picks where a project is mandatory.</summary>
    [Parameter] public bool AllowNone { get; set; } = true;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public string ToggleTitle { get; set; } = "Choose the project";

    private DropdownMenu? menu;

    protected override async Task OnInitializedAsync()
    {
        StageFilter.OnChange += FilterChanged;
        await StageFilter.EnsureLoadedAsync();
    }

    private List<Project> ActiveProjects =>
        Projects.InWorkOrder().Where(project => project.Stage != ProjectStage.Completed).ToList();

    private List<Project> CompletedProjects =>
        Projects.InWorkOrder().Where(project => project.Stage == ProjectStage.Completed).ToList();

    private string ToggleLabel =>
        Projects.FirstOrDefault(project => project.ProjectId.Equals(Value, StringComparison.OrdinalIgnoreCase))
            is { } picked ? $"{picked.Reference} — {picked.Name}" : Placeholder;

    private static string RowClass(bool isPicked) =>
        "w-full flex items-center gap-2.5 px-3 py-2 text-sm text-left transition "
        + (isPicked ? "bg-surface-raised" : "hover:bg-surface-raised");

    private Task PickAsync(string projectId)
    {
        menu?.Close();
        return ValueChanged.InvokeAsync(projectId);
    }

    private void FilterChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => StageFilter.OnChange -= FilterChanged;
}
