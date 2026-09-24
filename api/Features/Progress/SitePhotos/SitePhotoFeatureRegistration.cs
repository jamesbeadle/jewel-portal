using Jewel.JPMS.Contracts.Progress;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

internal static class SitePhotoFeatureRegistration
{
    public static void AddSitePhotos(this IServiceCollection services)
    {
        services.AddScoped<SitePhotoIntake>();

        services.AddScoped<IQueryHandler<ListSitePhotos, IReadOnlyList<SitePhoto>>, ListSitePhotosHandler>();
        services.AddScoped<IQueryHandler<MatchSitePhotos, SitePhotoMatches>, MatchSitePhotosHandler>();

        services.AddScoped<ICommandHandler<FileSitePhotos, SitePhotoFilingResult>, FileSitePhotosHandler>();
        services.AddScoped<FileSitePhotosAuthorisation>();
        services.AddScoped<FileSitePhotosValidation>();

        services.AddScoped<ICommandHandler<DeleteSitePhoto, Acknowledgement>, DeleteSitePhotoHandler>();
        services.AddScoped<DeleteSitePhotoAuthorisation>();

        services.AddScoped<ICommandHandler<ArchiveSitePhotos, SitePhotoArchivingResult>, ArchiveSitePhotosHandler>();
        services.AddScoped<ArchiveSitePhotosAuthorisation>();
        services.AddScoped<ArchiveSitePhotosValidation>();

        services.AddScoped<ICommandHandler<RestoreSitePhoto, Acknowledgement>, RestoreSitePhotoHandler>();
        services.AddScoped<RestoreSitePhotoAuthorisation>();
    }
}
