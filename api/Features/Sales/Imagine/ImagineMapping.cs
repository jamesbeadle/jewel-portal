using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>Entities → the views both the public page and the lead page read.</summary>
internal static class ImagineMapping
{
    public static ImagineImageView ToView(this ImagineImageEntity image) =>
        new(image.ImageId, (ImagineImageKind)image.Kind, image.Title, image.Description, image.Order, image.Liked, image.Comment);

    public static ImagineRoundView ToView(this ImagineRoundEntity round, IEnumerable<ImagineImageEntity> images)
    {
        var rows = images.Where(image => image.RoundId == round.RoundId).OrderBy(image => image.Order).ThenBy(image => image.CreatedAt).ToList();
        return new ImagineRoundView(
            round.RoundId,
            round.Number,
            (ImagineRoundKind)round.Kind,
            round.Brief,
            round.BasedOnImageId,
            (ImagineRoundStatus)round.Status,
            round.Error,
            round.RequestedAt,
            round.CompletedAt,
            round.Observations,
            rows.Where(image => image.Kind == (int)ImagineImageKind.Photo).Select(ToView).ToList(),
            rows.Where(image => image.Kind == (int)ImagineImageKind.Concept).Select(ToView).ToList());
    }

    /// <summary>Every round of a lead with its images, oldest first.</summary>
    public static async Task<IReadOnlyList<ImagineRoundView>> RoundsForLeadAsync(JpmsContext context, string leadId, CancellationToken ct)
    {
        var rounds = await context.ImagineRounds.AsNoTracking()
            .Where(row => row.LeadId == leadId)
            .OrderBy(row => row.Number)
            .ToListAsync(ct);
        if (rounds.Count == 0) return Array.Empty<ImagineRoundView>();
        var images = await context.ImagineImages.AsNoTracking()
            .Where(row => row.LeadId == leadId)
            .ToListAsync(ct);
        return rounds.Select(round => round.ToView(images)).ToList();
    }
}
