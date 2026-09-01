using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.Architects.Commands;
using Jewel.JPMS.Api.Features.Clients;
using Jewel.JPMS.Api.Features.Clients.Commands;
using Jewel.JPMS.Api.Features.Directory.Commands;
using Jewel.JPMS.Api.Features.Leads.Commands;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SubcontractorsAndLeadsActions
{
    private static IEnumerable<AiAction> ContactsActions() => new AiAction[]
    {
        new AiAction(
            Name: "create_client",
            Area: "Contacts",
            Description: "Creates a global client account. The primary contact email captured here is "
                + "where request documents are addressed when this client is the selected party on a "
                + "project/request.",
            CommandType: typeof(CreateClient),
            ResultType: typeof(Client),
            AuthorisationType: typeof(CreateClientAuthorisation),
            ValidationType: typeof(CreateClientValidation),
            VisibleTo: ClientRoles.AllowedToManageClients,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true),

        new AiAction(
            Name: "update_client_contact",
            Area: "Contacts",
            Description: "Updates a client account's name and primary contact — changing where request "
                + "documents are addressed when this client is a project's party.",
            CommandType: typeof(UpdateClientContact),
            ResultType: typeof(Client),
            AuthorisationType: typeof(UpdateClientContactAuthorisation),
            ValidationType: typeof(UpdateClientContactValidation),
            VisibleTo: ClientRoles.AllowedToManageClients,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "clientId comes from the client listing (ListClients)."),

        new AiAction(
            Name: "create_architect",
            Area: "Contacts",
            Description: "Creates a global architect practice. The contact email captured here is where "
                + "RFIs and other request documents are addressed when this architect is the selected "
                + "party on a project/request.",
            CommandType: typeof(CreateArchitect),
            ResultType: typeof(Architect),
            AuthorisationType: typeof(CreateArchitectAuthorisation),
            ValidationType: typeof(CreateArchitectValidation),
            VisibleTo: ArchitectRoles.AllowedToManageArchitects,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true),

        new AiAction(
            Name: "update_architect",
            Area: "Contacts",
            Description: "Updates an architect practice's name and contact — changing where RFIs are "
                + "issued when this architect is a project's party.",
            CommandType: typeof(UpdateArchitect),
            ResultType: typeof(Architect),
            AuthorisationType: typeof(UpdateArchitectAuthorisation),
            ValidationType: typeof(UpdateArchitectValidation),
            VisibleTo: ArchitectRoles.AllowedToManageArchitects,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "architectId comes from the architect listing (ListArchitects)."),

        new AiAction(
            Name: "upsert_party_contact",
            Area: "Contacts",
            Description: "Adds or updates a person on a client's or architect's contact book, including "
                + "their default correspondence routing — this decides who receives Jewel's outbound "
                + "request correspondence for that party. Marking a contact primary makes them the "
                + "party's To correspondent (any previous primary is demoted).",
            CommandType: typeof(UpsertPartyContact),
            ResultType: typeof(PartyContact),
            AuthorisationType: typeof(PartyContactAuthorisation),
            ValidationType: typeof(UpsertPartyContactValidation),
            VisibleTo: PartyContactManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "partyKind is Client or Architect; partyId is the matching client/architect id "
                + "(ListClients / ListArchitects). A null/blank partyContactId inserts; a populated one "
                + "(from ListPartyContacts) updates in place."),

        new AiAction(
            Name: "remove_party_contact",
            Area: "Contacts",
            Description: "Deletes a person from a client's or architect's contact book permanently — they "
                + "stop receiving Jewel's outbound request correspondence for that party. There is no "
                + "undo.",
            CommandType: typeof(RemovePartyContact),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(PartyContactAuthorisation),
            ValidationType: null,
            VisibleTo: PartyContactManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which contact, by name and party, before calling. "
                + "partyContactId comes from ListPartyContacts."),

        // ── Directory & users ─────────────────────────────────────────────────────────────────

    };
}
