using Jewel.JPMS.Contracts.Registers;

namespace Jewel.JPMS.Features.Registers.Policies;

/// <summary>
/// The register's view of the policies: a row per published revision, opened to show its declaration
/// and every person asked to sign it. The rows are read afresh each time a revision is opened, so a
/// signature that came in by link since the page loaded is there.
/// </summary>
public partial class PublishedPoliciesPanel
{
    [Inject] private IQueryClient Queries { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyList<PolicyDocument> Documents { get; set; } = Array.Empty<PolicyDocument>();
    [Parameter] public bool CanPublish { get; set; }
    [Parameter] public bool CanSend { get; set; }
    [Parameter] public string? ChasingId { get; set; }
    [Parameter] public EventCallback<PolicyDocument> OnSendByLink { get; set; }
    [Parameter] public EventCallback<PolicySignOff> OnChase { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }

    private string? openDocumentId;
    private IReadOnlyList<PolicySignOff>? openRows;

    public async Task ReloadOpenAsync()
    {
        if (openDocumentId is null) return;
        openRows = await Queries.AskAsync(new ListPolicySignOffs(openDocumentId), CancellationToken.None);
        StateHasChanged();
    }

    private async Task ToggleAsync(string policyDocumentId)
    {
        var isClosing = openDocumentId == policyDocumentId;
        openDocumentId = isClosing ? null : policyDocumentId;
        openRows = null;
        if (isClosing) return;
        await ReloadOpenAsync();
    }
}
