using System.Reflection;
using Jewel.JPMS.Api.Features.DataProtection.Queries;
using Microsoft.Azure.Functions.Worker;
using Xunit;

namespace Jewel.JPMS.Tests;

// The Functions host keeps every route template that begins with admin/ for its own administration
// API: a function that claims one is logged as "the specified route conflicts with one or more
// built in routes" and never loaded, so its path answers 404 on every host. The dossier and
// anonymise endpoints sat there unseen until 27 Sep 2026; this keeps the prefix out for good.
public sealed class HttpTriggerRouteTests
{
    private const string PrefixTheHostReserves = "admin/";
    private const string DossierRoute = "data-protection/people/dossier";
    private const string AnonymiseRoute = "data-protection/people/anonymise";

    [Fact]
    public void NoHttpTriggerRoute_beginsWithThePrefixTheHostReserves()
    {
        var routesTheHostWouldRefuse = HttpTriggerRoutes()
            .Where(route => route.Template.StartsWith(PrefixTheHostReserves, StringComparison.OrdinalIgnoreCase))
            .Select(route => $"{route.FunctionName}: {route.Template}")
            .ToList();

        Assert.Empty(routesTheHostWouldRefuse);
    }

    [Fact]
    public void TheDataProtectionEndpoints_areRoutedUnderDataProtection()
    {
        var templates = HttpTriggerRoutes().ToDictionary(route => route.FunctionName, route => route.Template);

        Assert.Equal(DossierRoute, templates["GetPersonDossier"]);
        Assert.Equal(AnonymiseRoute, templates["AnonymisePerson"]);
    }

    private static IEnumerable<(string FunctionName, string Template)> HttpTriggerRoutes() =>
        typeof(GetPersonDossierEndpoint).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .SelectMany(RoutesOf);

    private static IEnumerable<(string FunctionName, string Template)> RoutesOf(MethodInfo method)
    {
        var function = method.GetCustomAttribute<FunctionAttribute>();
        if (function is null) yield break;
        foreach (var parameter in method.GetParameters())
        {
            var trigger = parameter.GetCustomAttribute<HttpTriggerAttribute>();
            if (trigger?.Route is { } template) yield return (function.Name, template);
        }
    }
}
