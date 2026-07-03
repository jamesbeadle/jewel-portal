namespace Jewel.JPMS.Models;

// Lifecycle of a Liquidated Damages (LADs) claim — the client's contractual claim against Jewel for
// late completion (the counterpart to Jewel's own NOD/EOT notices on the Claims view). Notified when
// the client first gives notice; Disputed while Jewel contests it; Agreed once the amount is settled
// commercially; Withdrawn if the client drops it (e.g. an EOT lands); Settled once paid/deducted.
public enum LadStatus
{
    Notified = 0,
    Disputed = 1,
    Agreed = 2,
    Withdrawn = 3,
    Settled = 4
}

// A Liquidated Damages claim recorded against a project. Owns a sequential "LAD-0001" reference
// which is also its mailbox tag stem, so an email tagged "JPMS/LAD-0001" is the claim's linked mail —
// the same live-read link mechanism the Request / To-do families use. Surfaces on the project
// Schedule tab's Claims view alongside the NOD/EOT requests.
public sealed record LadClaim(
    string LadClaimId,
    string ProjectId,
    string Reference,          // sequential human reference, e.g. "LAD-0001" (also the tag stem)
    string Title,
    string Description,
    DateTimeOffset? PeriodFrom, // start of the delay period the claim covers
    DateTimeOffset? PeriodTo,   // end of the delay period the claim covers
    int DaysClaimed,            // days of culpable delay the client is claiming for
    decimal RatePerWeek,        // contractual LADs rate (per week)
    decimal Amount,             // the amount claimed (usually days/7 × rate, but recorded as notified)
    LadStatus Status,
    DateTimeOffset RaisedAt,    // when the client's notice was given
    string CreatedByEmail);

public static class LadStatusExtensions
{
    public static string DisplayName(this LadStatus status) => status switch
    {
        LadStatus.Notified  => "Notified",
        LadStatus.Disputed  => "Disputed",
        LadStatus.Agreed    => "Agreed",
        LadStatus.Withdrawn => "Withdrawn",
        LadStatus.Settled   => "Settled",
        _ => status.ToString()
    };
}
