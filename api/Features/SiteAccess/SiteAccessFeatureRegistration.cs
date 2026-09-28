using Jewel.JPMS.Api.Features.SiteAccess.Commands;
using Jewel.JPMS.Api.Features.SiteAccess.Queries;
using Jewel.JPMS.Api.Features.SiteAccess.Site;
using Jewel.JPMS.Contracts.SiteAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.SiteAccess;

/// <summary>
/// The site drawing links: the QR posters' management (curated behind a session) and, under
/// <c>Site/</c>, the ENTIRE anonymous surface of the drawing system — an auditor asking what can be
/// reached without a session has exactly one folder to read.
/// </summary>
public static class SiteAccessFeatureRegistration
{
    public static IServiceCollection AddSiteAccessFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListSiteDrawingLinksForProject, IReadOnlyList<SiteDrawingLink>>, ListSiteDrawingLinksForProjectHandler>();

        services.AddScoped<ICommandHandler<CreateSiteDrawingLink, SiteDrawingLinkCreated>, CreateSiteDrawingLinkHandler>();
        services.AddScoped<CreateSiteDrawingLinkAuthorisation>();
        services.AddScoped<CreateSiteDrawingLinkValidation>();

        services.AddScoped<ICommandHandler<RevokeSiteDrawingLink, Acknowledgement>, RevokeSiteDrawingLinkHandler>();
        services.AddScoped<RevokeSiteDrawingLinkAuthorisation>();
        services.AddScoped<RevokeSiteDrawingLinkValidation>();

        services.AddScoped<SiteLinkResolver>();
        services.AddScoped<SiteDrawingReach>();
        services.AddScoped<SiteDrawingListing>();
        services.AddScoped<SiteLinkScans>();
        return services;
    }
}
