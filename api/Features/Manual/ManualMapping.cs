using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Manual;

internal static class ManualMapping
{
    private const char SlugSeparator = ',';

    public static ManualModule ToModel(this ManualModuleEntity entity, int acknowledgedCount) =>
        new(entity.ManualModuleId, entity.Code, entity.Title, entity.Purpose, entity.Body,
            entity.OwnerEmail, entity.ApproverEmail, (ManualModuleStatus)entity.Status,
            entity.Version, entity.PublishedVersion, entity.ApprovedAt, entity.ApprovedByEmail, entity.NextReviewAt,
            entity.Audience(), SplitSlugs(entity.LinkedFormSlugs), entity.LinkedStandards, entity.ChangeSummary,
            entity.SourceSections, entity.Sequence, entity.UpdatedByEmail, entity.UpdatedAt, acknowledgedCount);

    public static ManualPublishedModule ToPublishedModel(this ManualModuleEntity entity, DateTimeOffset? acknowledgedAt) =>
        new(entity.ManualModuleId, entity.Code, entity.Title, entity.Purpose, entity.PublishedBody,
            entity.PublishedVersion, entity.ApprovedAt ?? entity.UpdatedAt, entity.ApprovedByEmail, entity.OwnerEmail,
            entity.NextReviewAt, SplitSlugs(entity.LinkedFormSlugs), entity.LinkedStandards, entity.ChangeSummary,
            acknowledgedAt);

    public static ManualModuleVersion ToModel(this ManualModuleVersionEntity entity, int acknowledgedCount) =>
        new(entity.ManualModuleVersionId, entity.ManualModuleId, entity.Version, entity.Title, entity.Body,
            entity.ChangeSummary, entity.ApprovedByEmail, entity.ApprovedAt, entity.SupersededAt, acknowledgedCount);

    public static ManualAcknowledgement ToModel(this ManualAcknowledgementEntity entity) =>
        new(entity.ManualAcknowledgementId, entity.ManualModuleId, entity.Version, entity.Email,
            entity.TypedName, entity.AcknowledgedAt);

    public static ManualAudience Audience(this ManualModuleEntity entity) =>
        new(entity.IsForSiteManagers, entity.IsForHealthAndSafetyOfficer, entity.IsForForemen);

    public static void SetAudience(this ManualModuleEntity entity, ManualAudience audience)
    {
        entity.IsForSiteManagers = audience.IsForSiteManagers;
        entity.IsForHealthAndSafetyOfficer = audience.IsForHealthAndSafetyOfficer;
        entity.IsForForemen = audience.IsForForemen;
    }

    public static IReadOnlyList<string> SplitSlugs(string joined) =>
        joined.Split(SlugSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string JoinSlugs(IReadOnlyList<string> slugs) =>
        string.Join(SlugSeparator, slugs.Select(slug => slug.Trim()).Where(slug => slug.Length > 0).Distinct());

    public static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();
}
