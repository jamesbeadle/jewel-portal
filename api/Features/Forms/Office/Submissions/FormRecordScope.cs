namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>
/// The record-level scope of the office's writes on the forms, asked by the endpoints and by the
/// connector alike after the role gate: a form kept in a restricted store is worked only by that
/// store's readers, and a folder's retention dates are set only by someone who may read every form
/// filed in it — a leaving date decides when those forms are destroyed, right-to-work records first.
/// </summary>
internal static class FormRecordScope
{
    public static async Task<bool> MayActOnFormAsync(JpmsContext context, SignedInUser user, string formSubmissionId, CancellationToken cancellationToken)
    {
        var submission = await context.FormSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(row => row.FormSubmissionId == formSubmissionId, cancellationToken);
        return submission is not null && FormSubmissionReading.MayRead(user, submission);
    }

    public static async Task<bool> MayDateFolderAsync(JpmsContext context, SignedInUser user, string formFolderId, CancellationToken cancellationToken)
    {
        var formSlugs = await context.FormSubmissions.AsNoTracking()
            .Where(row => row.FormFolderId == formFolderId && row.DestroyedAt == null)
            .Select(row => row.FormSlug).Distinct().ToListAsync(cancellationToken);
        var stores = formSlugs.Select(StoreOf).Distinct();
        return stores.All(store => FormRoleSets.ReadersOf(store).IncludesAny(user.Roles));
    }

    private static FormEvidenceStore StoreOf(string formSlug) => FormCatalogue.For(formSlug)?.Store ?? FormEvidenceStore.General;
}
