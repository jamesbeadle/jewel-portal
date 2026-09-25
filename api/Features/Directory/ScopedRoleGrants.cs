namespace Jewel.JPMS.Api.Features.Directory;

/// <summary>
/// Keeps the linked external roles out of Admin → Users. A client or subcontractor login is made
/// by an invite from the account it belongs to, which links the login and scopes everything it
/// reads; granted by hand, the role arrives without the link and the person meets a portal of
/// refusals. A role the person already holds is kept — editing an invited login's other roles
/// never strips the one its invite gave. The architect is given by hand, with its projects
/// (ProjectAccessGrants, 2026-09-25).
/// </summary>
public sealed class ScopedRoleGrants
{
    private const string Refusal =
        "Client and subcontractor logins are made by \"Invite to portal\" on the client's or the "
        + "company's own record, which links the login to it. Remove the role here and invite them "
        + "from there.";

    private readonly JpmsContext context;

    public ScopedRoleGrants(JpmsContext context) { this.context = context; }

    public async Task<string?> RefusalAsync(string email, IEnumerable<Role> requested, CancellationToken cancellationToken)
    {
        var held = await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == email)
            .Select(row => (Role)row.Role)
            .ToListAsync(cancellationToken);
        var isAddingAnExternalRole = requested
            .Any(role => LoginRoles.ScopedByALink.Contains(role) && !held.Contains(role));
        return isAddingAnExternalRole ? Refusal : null;
    }
}
