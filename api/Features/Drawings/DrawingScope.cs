using Jewel.JPMS.Api.Features.Parties;

namespace Jewel.JPMS.Api.Features.Drawings;

/// <summary>
/// Which drawings a login may read. Staff and the subcontractor read the register as they always
/// have; a project's party — the architect following a drawing linked on their RFI — reads the
/// drawings of the projects their login was given and no other (PartyReads).
/// </summary>
internal static class DrawingScope
{
    public static Task<bool> MayReadProjectAsync(
        JpmsContext context, SignedInUser user, string projectId, CancellationToken cancellationToken) =>
        ReadsTheWholeRegister(user)
            ? Task.FromResult(true)
            : PartyReads.MayReadProjectAsync(context, user, projectId, cancellationToken);

    public static async Task<bool> MayReadDrawingAsync(
        JpmsContext context, SignedInUser user, string drawingId, CancellationToken cancellationToken)
    {
        if (ReadsTheWholeRegister(user)) return true;
        var projectId = await context.Drawings.AsNoTracking()
            .Where(drawing => drawing.DrawingId == drawingId)
            .Select(drawing => drawing.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);
        return projectId is not null && await PartyReads.MayReadProjectAsync(context, user, projectId, cancellationToken);
    }

    public static async Task<bool> MayReadRevisionAsync(
        JpmsContext context, SignedInUser user, string revisionId, CancellationToken cancellationToken)
    {
        if (ReadsTheWholeRegister(user)) return true;
        var drawingId = await context.DrawingRevisions.AsNoTracking()
            .Where(revision => revision.DrawingRevisionId == revisionId)
            .Select(revision => revision.DrawingId)
            .FirstOrDefaultAsync(cancellationToken);
        return drawingId is not null && await MayReadDrawingAsync(context, user, drawingId, cancellationToken);
    }

    private static bool ReadsTheWholeRegister(SignedInUser user) =>
        JpmsRoleSets.AllInternal.IncludesAny(user.Roles) || user.Roles.Contains(JpmsRoles.Subcontractor);
}
