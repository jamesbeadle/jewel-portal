using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Features.Requests;

/// <summary>One suggested scope line in the variation draft: ticked in or out, and the cost
/// centre its committed value lands on once ticked.</summary>
public sealed record VariationDraftLine(VoqDraftLine Line, bool Accepted, string CostCode = "");
