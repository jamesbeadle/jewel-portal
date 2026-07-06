namespace Jewel.JPMS.Models;

/// <summary>
/// How a person participates in a project's outbound request correspondence (RFIs and other
/// request documents). Integer values are pinned because they are stored on
/// PartyContactEntity.DefaultRouting and ProjectContactEntity.Routing.
/// </summary>
public enum CorrespondenceRouting
{
    /// <summary>On the contact book but not copied on request documents.</summary>
    None = 0,
    /// <summary>A primary correspondent — the document is addressed to them.</summary>
    To = 1,
    /// <summary>Copied openly on every issued request document.</summary>
    Cc = 2,
    /// <summary>Blind-copied. Never shown on the PDF, the client-facing activity trail, or any
    /// other client-visible surface.</summary>
    Bcc = 3
}

public static class CorrespondenceRoutingExtensions
{
    public static string DisplayName(this CorrespondenceRouting routing) => routing switch
    {
        CorrespondenceRouting.To   => "To",
        CorrespondenceRouting.Cc   => "CC",
        CorrespondenceRouting.Bcc  => "BCC",
        _ => "Off"
    };
}

/// <summary>
/// A person at a client account or architect practice — the party's communication preferences.
/// <see cref="DefaultRouting"/> is how they join correspondence on every project that corresponds
/// with the party (a project can override it per contact); the <see cref="IsPrimary"/> contact is
/// the party's To correspondent and supersedes the legacy single contact-email field.
/// </summary>
public sealed record PartyContact(
    string PartyContactId,
    PartyKind PartyKind,
    string PartyId,
    string Name,
    string Email,
    string? JobTitle,
    CorrespondenceRouting DefaultRouting,
    bool IsPrimary,
    DateTimeOffset CreatedAt);

/// <summary>One resolved recipient of a request document, with the routing it resolved to.</summary>
public sealed record CorrespondenceRecipient(
    string Name,
    string Email,
    CorrespondenceRouting Routing,
    string? RoleLabel = null,
    string? Organisation = null);

/// <summary>
/// The full resolved recipient set for a request's outbound document: who it is addressed to, who
/// is copied, and who is blind-copied. Bcc exists only here and on the actual email — it is never
/// placed on the document model, the PDF, or the shared activity trail.
/// </summary>
public sealed record RequestRecipientSet(
    IReadOnlyList<CorrespondenceRecipient> To,
    IReadOnlyList<CorrespondenceRecipient> Cc,
    IReadOnlyList<CorrespondenceRecipient> Bcc)
{
    public static readonly RequestRecipientSet Empty = new(
        Array.Empty<CorrespondenceRecipient>(),
        Array.Empty<CorrespondenceRecipient>(),
        Array.Empty<CorrespondenceRecipient>());

    public bool HasTo => To.Count > 0;
}
