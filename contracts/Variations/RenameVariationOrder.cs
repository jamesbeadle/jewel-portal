using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Retitles a variation order. A wording correction on the record itself — the title a user reads
/// next to V72 — allowed at EVERY stage, approved ones included: a badly worded title is worth
/// fixing whenever it is spotted, and the number, not the wording, is what the client's paperwork
/// is keyed to.
///
/// Nothing already written downstream is rewritten. The valuation report lines, the CVR accruals
/// and the cost-centre commitments approval wrote carry the title AS IT READ AT THE MOMENT THEY
/// WERE WRITTEN — they are snapshots of a claim that has been issued, so re-wording them after the
/// fact would falsify a document the client already holds. Only writes made AFTER the rename carry
/// the new title.
///
/// Scope is deliberately the title alone: Description, value, lines and status each have their own
/// command, so a retitle can never be the thing that quietly moved a figure.
/// </summary>
public sealed record RenameVariationOrder(string VariationOrderId, string Title) : ICommand<VariationOrder>;
