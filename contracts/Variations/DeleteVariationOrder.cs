using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Deletes a variation order that was raised in error — a wrong or abandoned VOQ. Cascades its
/// quoting-stage tender data (bid packages and everything under them: invited subcontractors, scope
/// lines, quotes and their lines, linked drawings) and unlinks any subcontractor variation request
/// that pointed at it, so the request returns to the review queue rather than dangling.
///
/// Refused for an APPROVED order — an approved variation is on the Valuation Report, CVR and
/// cost-centre budget, so it must be rejected or returned to quoting first (which reverses those
/// writes) — and while any work order instructs it.
/// </summary>
public sealed record DeleteVariationOrder(string VariationOrderId) : ICommand<Acknowledgement>;
