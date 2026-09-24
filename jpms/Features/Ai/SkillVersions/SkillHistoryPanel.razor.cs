namespace Jewel.JPMS.Features.Ai.SkillVersions;

public partial class SkillHistoryPanel
{
    private const string SkillItself = "";

    [Parameter, EditorRequired] public string SkillKey { get; set; } = "";
    /// <summary>Moves whenever the page's copy of the skill changes — a save, a reference save, a
    /// restore — and the history reloads.</summary>
    [Parameter] public int Revision { get; set; }
    [Parameter] public EventCallback OnRestored { get; set; }

    private SkillHistory? history;
    private bool hasFailed;
    private string documentKey = SkillItself;
    private int? shownVersion;
    private SkillVersion? restoreArmed;
    private bool isRestoring;
    private string? restoreError;
    private int loadedFor;

    private IReadOnlyList<SkillVersion> Versions =>
        history is null ? Array.Empty<SkillVersion>()
        : documentKey == SkillItself ? history.Versions
        : history.References.FirstOrDefault(reference => reference.RefKey == documentKey)?.Versions
          ?? Array.Empty<SkillVersion>();

    private SkillVersion? Shown => Versions.FirstOrDefault(version => version.Version == shownVersion);

    private IReadOnlyList<TabItem> DocumentChoices =>
        new[] { new TabItem(SkillItself, "The skill", Count: history!.Versions.Count) }
            .Concat(history.References.Select(reference =>
                new TabItem(reference.RefKey, reference.DisplayName, Count: reference.Versions.Count)))
            .ToList();

    private string RestoreTitle => $"Restore version {restoreArmed?.Version}?";

    protected override async Task OnParametersSetAsync()
    {
        if (loadedFor == Revision && history is not null) return;
        loadedFor = Revision;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        hasFailed = false;
        try
        {
            history = await Queries.AskAsync(new GetAiSkillHistory(SkillKey), CancellationToken.None);
            hasFailed = history is null;
        }
        catch
        {
            hasFailed = true;
        }
    }

    private void ShowDocument(string key)
    {
        documentKey = key;
        shownVersion = null;
    }

    private void ShowVersion(int version) => shownVersion = version;

    private async Task RestoreAsync()
    {
        if (restoreArmed is null || isRestoring) return;
        isRestoring = true;
        restoreError = null;
        try
        {
            var refKey = documentKey == SkillItself ? null : documentKey;
            await Commands.SendAsync(
                new RestoreAiSkillVersion(SkillKey, refKey, restoreArmed.Version, string.Empty), CancellationToken.None);
            restoreArmed = null;
            shownVersion = null;
            await OnRestored.InvokeAsync();
        }
        catch (CommandFailedException refusal)
        {
            restoreError = refusal.Message;
        }
        finally
        {
            isRestoring = false;
        }
    }
}
