using Jewel.JPMS.Contracts.Labour;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Features.Labour.MyDay;

/// <summary>The log form's draft: what the worker has typed, kept on the phone as they type and
/// read back when the card opens, so a refresh or a trip to the camera never loses the day's
/// words. Cleared once the day is saved. The photographs are the one thing it cannot keep.</summary>
public partial class MyDayLogForm
{
    [Inject] private MyDayDraftStore Drafts { get; set; } = default!;

    private bool isRestored;

    private MyDayDraft CurrentDraft => new(hours, costCode, description, instruction, defect, leftAt, siteProjectId);

    private async Task RestoreDraftAsync()
    {
        var draft = await Drafts.ReadAsync(Project.ProjectId);
        if (draft is not { HasAnything: true }) return;
        if (Projects.Any(project => project.ProjectId == draft.SiteProjectId)) siteProjectId = draft.SiteProjectId;
        hours = draft.Hours;
        costCode = draft.CostCode;
        description = draft.Description;
        instruction = draft.Instruction;
        defect = draft.Defect;
        isDefectBoxOpen = !string.IsNullOrWhiteSpace(draft.Defect);
        leftAt = draft.LeftAt;
        isRestored = true;
    }

    private Task SaveDraftAsync() => Drafts.WriteAsync(Project.ProjectId, CurrentDraft);

    private Task ForgetDraftAsync() => Drafts.ClearAsync(Project.ProjectId);
}
