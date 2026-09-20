
namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>Who may queue extractions — the same set that may upload drawings (see
/// UploadDrawingRevisionAuthorisation) — and who may read what an extraction produced.
///
/// Reading needed only a sign-in until 2026-09-19, which made the structured form of a drawing
/// more open than the drawing: the file at the same revision is JpmsRoleSets.DrawingReaders, which
/// excludes the client, the site operative and accounts, while the extraction handed its title
/// block, figured dimensions and callouts to any signed-in user for any project. The connector's
/// query_document_data already read it as DrawingReaders; the route now agrees with both.</summary>
public static class DrawingExtractionRoles
{
    public static readonly RoleSet AllowedToExtract =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    public static readonly RoleSet AllowedToReadExtractions = JpmsRoleSets.DrawingReaders;
}
