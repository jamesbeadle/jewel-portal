using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Un-issues a Variation Order: Issued back to Approved, clearing the issued date — for a VO marked
/// Issued in error. Purely a record correction (issuing writes no commercial figures), unlike
/// Cancel, which reverses the approval's valuation/CVR/budget writes and is not undone this way.
/// </summary>
public sealed record RevertVariationOrderToApproved(string VariationOrderId) : ICommand<VariationOrder>;
