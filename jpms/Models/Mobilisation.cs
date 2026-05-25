namespace Jewel.JPMS.Models;

public sealed record MobilisationItem(
    string MobilisationItemId,
    string ProjectId,
    string Description,
    string OwnerEmail,
    bool IsComplete,
    DateTimeOffset? CompletedAt);

public sealed record MobilisationChecklist(
    string ProjectId,
    IReadOnlyList<MobilisationItem> Items)
{
    public bool IsGateOpen => Items.All(item => item.IsComplete);
    public int CompletedCount => Items.Count(item => item.IsComplete);
}
