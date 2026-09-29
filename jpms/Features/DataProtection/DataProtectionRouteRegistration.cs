using Jewel.JPMS.Contracts.DataProtection;

namespace Jewel.JPMS.Features.DataProtection;

public static class DataProtectionRouteRegistration
{
    private const string DossierPath = "/api/data-protection/people/dossier";
    private const string AnonymisePath = "/api/data-protection/people/anonymise";

    public static void RegisterDataProtectionRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<GetPersonDossier, PersonDossier>(
            new QueryRoute(DossierPath,
                query => $"{DossierPath}?email={Uri.EscapeDataString(((GetPersonDossier)query).Email)}"));

        commands.Register<AnonymisePerson, PersonAnonymisation>(
            new CommandRoute("POST", AnonymisePath, _ => AnonymisePath));
    }
}
