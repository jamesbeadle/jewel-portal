namespace Jewel.JPMS.Models;

/// <summary>Where a one-time link has got to, as the office reads it on Sent out and on a pack.</summary>
public enum FormLinkState
{
    NotOpened = 0,
    Opened = 1,
    Done = 2,
    Expired = 3,
    Replaced = 4
}

/// <summary>How the person is engaged. Persisted as an int: append, never reorder.</summary>
public enum Engagement
{
    Employee = 0,
    SelfEmployed = 1
}

/// <summary>
/// One form sent to one named person through a link that is theirs alone (lib/invites.js in the
/// dashboard): who it went to, who sent it and when, when it was opened and when it was used.
/// The invite row is the evidential record — the name the person typed is only a prefill.
/// </summary>
public sealed record FormInvite(
    string FormInviteId,
    string? FormPackId,
    string FormSlug,
    JewelCompany Company,
    string PersonName,
    string CompanyName,
    string Email,
    string SentByName,
    DateTimeOffset SentAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? UsedAt,
    string? FormSubmissionId,
    DateTimeOffset? CancelledAt)
{
    public FormLinkState StateAt(DateTimeOffset now) => this switch
    {
        { CancelledAt: not null } => FormLinkState.Replaced,
        { UsedAt: not null } => FormLinkState.Done,
        _ when ExpiresAt < now => FormLinkState.Expired,
        { OpenedAt: not null } => FormLinkState.Opened,
        _ => FormLinkState.NotOpened
    };
}

/// <summary>What the office tells the portal when it sends a pack; the portal decides the forms from it.</summary>
public sealed record FormPackAnswers(bool HasP45, bool IsWorkingAtAScreen, bool IsGettingAVehicle, bool MustHoldATicket);

/// <summary>
/// One new starter's forms behind one link. Each form keeps its own invite, so the opened, used and
/// expired states still hold per form and one form can be sent again without the pack.
/// </summary>
public sealed record FormPack(
    string FormPackId,
    JewelCompany Company,
    string PersonName,
    string Email,
    Engagement EngagedAs,
    string SentByName,
    DateTimeOffset SentAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? LastChasedAt,
    int ChaseCount,
    DateTimeOffset? CancelledAt,
    IReadOnlyList<FormInvite> Forms)
{
    public IReadOnlyList<FormInvite> CurrentForms => Forms.Where(form => form.CancelledAt is null).ToList();

    public IReadOnlyList<FormInvite> OutstandingForms => CurrentForms.Where(form => form.UsedAt is null).ToList();

    public bool IsLiveAt(DateTimeOffset now) => CancelledAt is null && CompletedAt is null && ExpiresAt >= now;
}
