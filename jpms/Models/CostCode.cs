namespace Jewel.JPMS.Models;

public sealed record CostCode(
    string CostCodeId,
    string ProjectId,
    string Code,
    string Description);
