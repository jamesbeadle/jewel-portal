using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Features.UsefulInformation;

// Client routes for Useful Information notes. Mirrors the api endpoints in
// Features/UsefulInformation: list + add are project-scoped, update/delete address the note.
public static class UsefulInformationRouteRegistration
{
    public static void RegisterUsefulInformationRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListUsefulInformationForProject, IReadOnlyList<UsefulInformationNote>>(
            new QueryRoute("/api/projects/{projectId}/useful-information",
                query => $"/api/projects/{((ListUsefulInformationForProject)query).ProjectId}/useful-information"));

        commands.Register<AddUsefulInformationNote, UsefulInformationNote>(
            new CommandRoute("POST", "/api/projects/{projectId}/useful-information",
                command => $"/api/projects/{((AddUsefulInformationNote)command).ProjectId}/useful-information"));

        commands.Register<UpdateUsefulInformationNote, UsefulInformationNote>(
            new CommandRoute("PUT", "/api/useful-information-notes/{noteId}",
                command => $"/api/useful-information-notes/{((UpdateUsefulInformationNote)command).UsefulInformationNoteId}"));

        commands.Register<DeleteUsefulInformationNote, Acknowledgement>(
            new CommandRoute("DELETE", "/api/useful-information-notes/{noteId}",
                command => $"/api/useful-information-notes/{((DeleteUsefulInformationNote)command).UsefulInformationNoteId}"));
    }
}
