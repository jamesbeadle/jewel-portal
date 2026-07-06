namespace Jewel.JPMS.Models;

/// <summary>
/// The outcome of drafting a tender-invite email in the shared mailbox: the package it belongs to,
/// who the draft BCCs, and where to open it. <see cref="WebLink"/> opens the draft in Outlook on
/// the web when Graph returns one (it usually does); null otherwise — the draft is still in the
/// mailbox's Drafts folder. Showing Bcc here is correct because the person reviewing the draft is
/// internal (Bcc stays off every subcontractor-facing surface).
/// </summary>
public sealed record BidPackageInviteDraft(
    BidPackage Package,
    string Subject,
    IReadOnlyList<string> Bcc,
    string? WebLink);
