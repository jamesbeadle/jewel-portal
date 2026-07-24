using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// A single priced line making up a variation's value — the build-up the approve panel captures.
/// Each becomes a Variation line on the Valuation Report under the minted V-ref, coded to its own
/// cost centre. The line amount is Quantity × Rate; a negative amount is stored as an omit (work
/// removed from the contract scope).
/// </summary>
public sealed record VariationLineInput(
    string CostCode,
    string Description,
    decimal Quantity,
    decimal Rate);

/// <summary>
/// Approves a variation order — the client's instruction to proceed. In one transaction it mints
/// the V-ref, records the agreed value and cost code on the order, writes the Variation line(s)
/// into the Valuation Report, records a QS accrual on the CVR and commits the value to the
/// cost-centre budget(s), then marks the order Approved.
///
/// Lines carries the priced build-up. When it is present, each line is written to the report under
/// its own cost centre, the order's Value is the sum of the lines and its CostCode the first line's
/// (the primary centre), and each cost centre's budget is committed its own share. When Lines is
/// null/empty the legacy single-line behaviour applies: Value (defaulting to the order's estimate)
/// against the single CostCode. Allowed from Quoting (a manual/internal approval) as well as Issued.
/// </summary>
public sealed record ApproveVariationOrder(
    string VariationOrderId,
    string CostCode,
    string ApprovedByEmail,
    decimal? Value = null,
    IReadOnlyList<VariationLineInput>? Lines = null) : ICommand<VariationOrder>;
