
namespace Jewel.JPMS.Features.Manual;

/// <summary>One form for a new module and for editing a draft: the same fields, mirroring the api's limits.</summary>
public partial class ManualModuleForm
{
    [Inject] private ICommandSender Commands { get; set; } = default!;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public ManualModule? Editing { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<ManualModule> OnSaved { get; set; }

    private string code = "";
    private string title = "";
    private string purpose = "";
    private string body = "";
    private string ownerEmail = "";
    private string approverEmail = "";
    private DateTime? nextReview;
    private bool isForSiteManagers = true;
    private bool isForHealthAndSafetyOfficer = true;
    private bool isForForemen = true;
    private HashSet<string> linkedFormSlugs = new();
    private string linkedStandards = "";
    private string changeSummary = "";
    private string sourceSections = "";
    private bool isSaving;
    private string? problem;
    private string? loadedForModuleId;

    private bool IsEdit => Editing is not null;

    protected override void OnParametersSet()
    {
        if (Editing is null || loadedForModuleId == Editing.ManualModuleId) return;
        loadedForModuleId = Editing.ManualModuleId;
        Load(Editing);
    }

    private void Load(ManualModule module)
    {
        title = module.Title;
        purpose = module.Purpose;
        body = module.Body;
        ownerEmail = module.OwnerEmail;
        approverEmail = module.ApproverEmail;
        nextReview = DateOf(module.NextReviewAt);
        var audience = module.Audience;
        isForSiteManagers = audience.IsForSiteManagers;
        isForHealthAndSafetyOfficer = audience.IsForHealthAndSafetyOfficer;
        isForForemen = audience.IsForForemen;
        linkedFormSlugs = module.LinkedFormSlugs.ToHashSet();
        linkedStandards = module.LinkedStandards;
        changeSummary = module.ChangeSummary;
    }

    private static DateTime? DateOf(DateTimeOffset? at) => at?.Date;

    private void ToggleForm(string slug, bool isLinked)
    {
        if (isLinked) { linkedFormSlugs.Add(slug); return; }
        linkedFormSlugs.Remove(slug);
    }

    private ManualAudience Audience => new(isForSiteManagers, isForHealthAndSafetyOfficer, isForForemen);

    private DateTimeOffset? NextReviewAt => nextReview is { } date ? new DateTimeOffset(date, TimeSpan.Zero) : null;

    private async Task SaveAsync()
    {
        problem = string.IsNullOrWhiteSpace(title) ? "Give the module a title." : null;
        if (problem is not null) return;
        isSaving = true;
        try
        {
            var saved = IsEdit ? await UpdateAsync() : await CreateAsync();
            loadedForModuleId = null;
            await OnSaved.InvokeAsync(saved);
        }
        catch (CommandFailedException refusal) { problem = refusal.Message; }
        finally { isSaving = false; }
    }

    private Task<ManualModule> CreateAsync() =>
        Commands.SendAsync(new CreateManualModule(code.Trim().ToUpperInvariant(), title, purpose, body, ownerEmail, approverEmail,
            Audience, linkedFormSlugs.ToList(), linkedStandards, sourceSections, NextReviewAt), CancellationToken.None);

    private Task<ManualModule> UpdateAsync() =>
        Commands.SendAsync(new UpdateManualModuleDraft(Editing!.ManualModuleId, title, purpose, body, ownerEmail, approverEmail,
            Audience, linkedFormSlugs.ToList(), linkedStandards, changeSummary, NextReviewAt), CancellationToken.None);
}
