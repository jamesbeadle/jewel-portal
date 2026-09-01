using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>Who may queue extractions — the same set that may upload drawings (see
/// UploadDrawingRevisionAuthorisation). Reading a data view needs only a sign-in.</summary>
public static class DrawingExtractionRoles
{
    public static readonly RoleSet AllowedToExtract =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);
}
