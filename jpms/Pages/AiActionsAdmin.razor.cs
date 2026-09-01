using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Pages;

public partial class AiActionsAdmin
{
    private bool sessionReady;
    private bool dataFailed;
    private bool saving;
    private string search = "";

    // Null means no fetch has landed — the panel reveals in one piece, gated on both
    // (CLAUDE.md loading states).
    private AiActionCatalogue? catalogue;
    private IReadOnlyList<SkillSummary>? skills;
    private List<AiActionSkillAttachment> attachments = new();

    private string? editingKind;
    private string? editingKey;
    private HashSet<string> working = new();
    private List<string> saveErrors = new();
    private readonly HashSet<string> expandedAreas = new(StringComparer.OrdinalIgnoreCase);

    // Mirrors the API's SkillRoles.ManageSkills — the board plus administrators. Nav visibility is
    // not permission; the endpoint gates for real.
    private bool CanSee =>
        Session.ActiveRole is Role.Admin or Role.ManagingDirector or Role.FinanceDirector;

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        sessionReady = true;
        if (!CanSee) return;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        dataFailed = false;
        catalogue = null;
        skills = null;
        try
        {
            catalogue = await Queries.AskAsync(new GetAiActionCatalogue(), CancellationToken.None);
            skills = await Queries.AskAsync(new ListAiSkills(), CancellationToken.None);
            attachments = catalogue.Attachments.ToList();
        }
        catch
        {
            dataFailed = true;
        }
    }

    private IEnumerable<IGrouping<string, AiActionSummary>> FilteredAreas =>
        catalogue!.Actions
            .Where(MatchesSearch)
            .GroupBy(action => action.Area)
            .OrderBy(group => group.Key);

    private bool MatchesSearch(AiActionSummary action) =>
        string.IsNullOrWhiteSpace(search)
        || action.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
        || action.Summary.Contains(search, StringComparison.OrdinalIgnoreCase)
        || action.Area.Contains(search, StringComparison.OrdinalIgnoreCase);

    // A live search opens every matching area — collapsed results would read as "not found".
    private bool IsExpanded(string area) =>
        !string.IsNullOrWhiteSpace(search) || expandedAreas.Contains(area);

    private void ToggleArea(string area)
    {
        if (!expandedAreas.Remove(area)) expandedAreas.Add(area);
    }

    private void OnSearchInput(ChangeEventArgs args) => search = args.Value?.ToString() ?? "";

    private IReadOnlyList<string> AttachedTo(string kind, string key) =>
        attachments
            .Where(row => row.TargetKind == kind && string.Equals(row.TargetKey, key, StringComparison.OrdinalIgnoreCase))
            .Select(row => row.SkillKey)
            .OrderBy(skillKey => skillKey)
            .ToList();

    private IReadOnlyList<AiActionSkillAttachment> OrphanedAttachments =>
        attachments
            .Where(row =>
                (row.TargetKind == AiActionSkillTargets.Action
                    && !catalogue!.Actions.Any(action => string.Equals(action.Name, row.TargetKey, StringComparison.OrdinalIgnoreCase)))
                || (row.TargetKind == AiActionSkillTargets.Area
                    && !catalogue!.Actions.Any(action => string.Equals(action.Area, row.TargetKey, StringComparison.OrdinalIgnoreCase))))
            .ToList();

    private SkillSummary? SkillFor(string skillKey) =>
        skills!.FirstOrDefault(skill => skill.SkillKey == skillKey);

    private bool IsEditing(string kind, string key) =>
        editingKind == kind && string.Equals(editingKey, key, StringComparison.OrdinalIgnoreCase);

    private void StartEditing(string kind, string key)
    {
        editingKind = kind;
        editingKey = key;
        working = AttachedTo(kind, key).ToHashSet();
        saveErrors.Clear();
    }

    private void CancelEditing()
    {
        editingKind = null;
        editingKey = null;
        saveErrors.Clear();
    }

    private void ToggleSkill(string skillKey)
    {
        if (!working.Remove(skillKey)) working.Add(skillKey);
    }

    private async Task SaveEditingAsync()
    {
        if (editingKind is null || editingKey is null || saving) return;
        await SaveTargetAsync(editingKind, editingKey, working.OrderBy(skillKey => skillKey).ToList());
        if (saveErrors.Count == 0) CancelEditing();
    }

    private async Task DetachOrphanAsync(AiActionSkillAttachment orphan)
    {
        var remaining = AttachedTo(orphan.TargetKind, orphan.TargetKey)
            .Where(skillKey => skillKey != orphan.SkillKey)
            .ToList();
        await SaveTargetAsync(orphan.TargetKind, orphan.TargetKey, remaining);
    }

    private async Task SaveTargetAsync(string kind, string key, IReadOnlyList<string> skillKeys)
    {
        saving = true;
        saveErrors.Clear();
        try
        {
            await Commands.SendAsync(
                new SaveAiActionSkills(kind, key, skillKeys, string.Empty),
                CancellationToken.None);

            // Mirror the save locally — same rows the next describe_action will resolve.
            attachments.RemoveAll(row =>
                row.TargetKind == kind && string.Equals(row.TargetKey, key, StringComparison.OrdinalIgnoreCase));
            attachments.AddRange(skillKeys.Select(skillKey => new AiActionSkillAttachment(
                kind, key, skillKey, Auth.CurrentUser?.Email ?? "", DateTimeOffset.UtcNow)));
        }
        catch (CommandFailedException failure)
        {
            saveErrors = new List<string> { failure.Message };
        }
        catch
        {
            saveErrors = new List<string> { "The attachments could not be saved. The error toast carries the reference." };
        }
        finally
        {
            saving = false;
        }
    }


}
