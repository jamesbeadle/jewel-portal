using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Parties;

/// <summary>The people at a client account or architect practice, with each person's default
/// correspondence routing — the party's communication preferences.</summary>
public sealed record ListPartyContacts(PartyKind PartyKind, string PartyId)
    : IQuery<IReadOnlyList<PartyContact>>;

/// <summary>
/// Add or update a person on a party's contact book. A null/blank PartyContactId inserts; a
/// populated one updates in place. Marking a contact <see cref="IsPrimary"/> makes them the
/// party's To correspondent (any previous primary is demoted, and the party's legacy contact
/// email field is kept in step so older read paths keep resolving).
/// </summary>
public sealed record UpsertPartyContact(
    PartyKind PartyKind,
    string PartyId,
    string Name,
    string Email,
    CorrespondenceRouting DefaultRouting,
    bool IsPrimary,
    string? JobTitle = null,
    string? PartyContactId = null) : ICommand<PartyContact>;

public sealed record RemovePartyContact(PartyKind PartyKind, string PartyId, string PartyContactId)
    : ICommand<Acknowledgement>;
