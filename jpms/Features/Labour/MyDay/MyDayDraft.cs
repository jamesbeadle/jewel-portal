using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Features.Labour.MyDay;

/// <summary>What the worker has typed on the log form and not yet submitted — everything but the
/// photographs, which a browser cannot keep across a refresh. Kept on the phone so a refresh, a
/// dropped tab or a trip to the camera never loses the day's words.</summary>
public sealed record MyDayDraft(
    decimal Hours,
    string CostCode,
    string Description,
    SiteLogInstruction? Instruction,
    string Defect,
    string LeftAt)
{
    public bool HasAnything =>
        !string.IsNullOrWhiteSpace(Description) || Instruction is not null || !string.IsNullOrWhiteSpace(Defect);
}
