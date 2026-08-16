using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// ---- The in-app tender-invite composer (2026-08-16, replacing the review-in-Outlook flow) ----
//
// The invite is composed, edited and SENT on the bid package page, like a triage reply — no trip
// to Outlook. Recipients: the tender list is pre-filled into BCC (subcontractors must never see
// each other) and the whole envelope stays editable; an empty To is addressed to the projects
// mailbox itself, the house convention for BCC-fan-out emails. What travels with it — the
// generated pricing schedule, the company Terms & Conditions (Admin → System), the package's
// tender documents and linked drawings, with the 25 MB overflow-to-links rule — is planned
// server-side by the same assembler as the Outlook-draft path.
//
// Recipient lists cross the wire as semicolon-separated address strings — exactly what the
// composer's fields hold, so what you read is what is sent.

/// <summary>Sends the tender-invite email from the shared projects mailbox, there and then. On a
/// send failure the staged draft survives in the mailbox's Drafts folder (Sent=false, FailureNote
/// says so) — an invite can never be lost between the portal and the mailbox.</summary>
public sealed record SendBidPackageInvite(
    string BidPackageId,
    string Subject,
    string HtmlBody,
    string To = "",
    string Cc = "",
    string Bcc = "") : ICommand<BidPackageInviteSendOutcome>;

public sealed record BidPackageInviteSendOutcome(
    BidPackage Package,
    bool Sent,
    string? WebLink,
    int RecipientCount,
    IReadOnlyList<string> LinkedFiles,
    string? FailureNote = null);

/// <summary>The composer's persisted working state — saved on the PACKAGE, so anyone on the team
/// can pick the draft up later from any browser. Null Subject/Body/recipients = never saved.</summary>
public sealed record BidPackageInviteComposerDraft(
    string Subject,
    string Body,
    string To,
    string Cc,
    string Bcc,
    DateTimeOffset SavedAt);

public sealed record GetBidPackageInviteComposerDraft(string BidPackageId) : IQuery<BidPackageInviteComposerDraft?>;

/// <summary>Saves (or overwrites) the composer draft on the package. Sending clears it — a sent
/// invite's draft has served its purpose.</summary>
public sealed record SaveBidPackageInviteComposerDraft(
    string BidPackageId,
    string Subject,
    string Body,
    string To = "",
    string Cc = "",
    string Bcc = "") : ICommand<Acknowledgement>;
