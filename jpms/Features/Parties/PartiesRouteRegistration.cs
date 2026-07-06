using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Parties;

// Party contact books: the people at a client account or architect practice, with each person's
// default To/CC/BCC routing (the party's communication preferences).
public static class PartiesRouteRegistration
{
    public static void RegisterPartiesRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListPartyContacts, IReadOnlyList<PartyContact>>(
            new QueryRoute("/api/parties/{partyKind}/{partyId}/contacts",
                query =>
                {
                    var q = (ListPartyContacts)query;
                    return $"/api/parties/{Segment(q.PartyKind)}/{q.PartyId}/contacts";
                }));

        commands.Register<UpsertPartyContact, PartyContact>(
            new CommandRoute("POST", "/api/parties/{partyKind}/{partyId}/contacts",
                command =>
                {
                    var c = (UpsertPartyContact)command;
                    return $"/api/parties/{Segment(c.PartyKind)}/{c.PartyId}/contacts";
                }));

        commands.Register<RemovePartyContact, Acknowledgement>(
            new CommandRoute("DELETE", "/api/parties/{partyKind}/{partyId}/contacts/{partyContactId}",
                command =>
                {
                    var c = (RemovePartyContact)command;
                    return $"/api/parties/{Segment(c.PartyKind)}/{c.PartyId}/contacts/{c.PartyContactId}";
                }));
    }

    private static string Segment(PartyKind kind) =>
        kind == PartyKind.Architect ? "architect" : "client";
}
