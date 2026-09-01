using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Features.WeeklyCashflow;

/// <summary>One supplier group's slice of the plan: the group and the bills it pulls out of the
/// flat list into one combined row.</summary>
public sealed record GroupSlice(WeeklyCashflowSupplierGroup Group, IReadOnlyList<WeeklyCashflowEntry> Entries);
