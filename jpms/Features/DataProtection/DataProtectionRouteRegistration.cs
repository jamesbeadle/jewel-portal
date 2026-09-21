using Jewel.JPMS.Contracts.DataProtection;

namespace Jewel.JPMS.Features.DataProtection;

public static class DataProtectionRouteRegistration
{
    public static void RegisterDataProtectionRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<GetPersonDossier, PersonDossier>(
            new QueryRoute("/api/admin/people/dossier",
                query => $"/api/admin/people/dossier?email={Uri.EscapeDataString(((GetPersonDossier)query).Email)}"));

        commands.Register<AnonymisePerson, PersonAnonymisation>(
            new CommandRoute("POST", "/api/admin/people/anonymise", _ => "/api/admin/people/anonymise"));
    }
}
