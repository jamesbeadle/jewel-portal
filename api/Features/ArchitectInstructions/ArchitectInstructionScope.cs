using Jewel.JPMS.Api.Features.Architects;

namespace Jewel.JPMS.Api.Features.ArchitectInstructions;

/// <summary>
/// Which instruction a caller may file or change. The role gate admits the architect so they can
/// file their own instruction rather than emailing it; this answers "on whose job?" — an
/// architect files and changes instructions only on the projects their login was given
/// (ArchitectProjects). The internal team's role is the whole answer.
/// </summary>
internal static class ArchitectInstructionScope
{
    public static async Task<bool> MayFileOnProjectAsync(
        JpmsContext context, SignedInUser user, string projectId, CancellationToken cancellationToken)
    {
        if (JpmsRoleSets.AllInternal.IncludesAny(user.Roles)) return true;
        var architectLogin = ArchitectScope.OwnArchitectLogin(user);
        if (architectLogin is null) return false;
        return await ArchitectProjects.OwnsProjectAsync(context, architectLogin, projectId, cancellationToken);
    }

    public static async Task<bool> MayActOnAsync(
        JpmsContext context, SignedInUser user, string instructionId, CancellationToken cancellationToken)
    {
        if (JpmsRoleSets.AllInternal.IncludesAny(user.Roles)) return true;
        var architectLogin = ArchitectScope.OwnArchitectLogin(user);
        if (architectLogin is null) return false;
        return await ArchitectProjects.OwnsInstructionAsync(context, architectLogin, instructionId, cancellationToken);
    }
}
