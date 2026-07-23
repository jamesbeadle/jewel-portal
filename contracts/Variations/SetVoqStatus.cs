using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Moves a VOQ directly between the side-effect-free stages of its lifecycle (Draft, Inviting,
/// Tendering, Selected, Rejected) — the status pill's dropdown on the VOQ page. Approval is
/// deliberately out of scope: approving raises a Variation Order and writes through to the
/// Valuation Report, CVR and cost-centre budget, so it must go through ApproveVariationOrderQuote;
/// likewise an already-approved VOQ can only leave Approved via ReturnVoqToTendering, which
/// reverses those writes. Selected can only be restored when a winning tender is already recorded
/// on the VOQ (SelectVoqTender records one).
/// </summary>
public sealed record SetVoqStatus(string VariationOrderQuoteId, VariationOrderQuoteStatus Status) : ICommand<VariationOrderQuote>;
