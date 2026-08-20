using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Procurement;

// Works out the trade to search subcontractors for FROM the bid package itself — its title, its
// specification summary and its line items — instead of making the user pick from a dropdown
// (decision 2026-08-20: the package already says what it is; a manual trade pick was pure
// friction, and on generic package trades like "Specialist" it was friction that produced a
// useless search term anyway). One cheap AI call per resolution; degrades to the package's own
// stored trade when the AI is unconfigured or fails, and to Ready=false when the package has no
// details to reason from — the same readiness rule that gates inviting subcontractors at all.
public sealed record ResolveBidPackageTrade(string BidPackageId) : IQuery<BidPackageTradeResolution>;

/// <summary>
/// <see cref="Ready"/> false means the package does not yet carry enough detail to invite
/// subcontractors — no title, or neither a specification summary nor any line items — and
/// <see cref="Reason"/> says what is missing, phrased for the user. When Ready is true,
/// <see cref="Trade"/> is the search-ready trade term; <see cref="UsedAi"/> false marks a degrade
/// (the package's own stored trade was used) and <see cref="Reason"/> then carries the note.
/// </summary>
public sealed record BidPackageTradeResolution(
    bool Ready,
    string? Trade = null,
    string? Reason = null,
    bool UsedAi = false);
