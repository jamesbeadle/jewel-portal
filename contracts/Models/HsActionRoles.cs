namespace Jewel.JPMS.Models;

/// <summary>
/// Who does what on a corrective action (Katy-Louise, 15 Sep 2026: "will need to stay open until
/// I check or have a photo confirmation"). Anyone who runs an audit may write on an action — the
/// site manager saying he is on it or what is stopping him, the officer answering — but only the
/// H&amp;S officer closes one, on her next visit or on a photograph, with the directors able to
/// stand in for her. A site manager never closes his own action.
/// </summary>
public static class HsActionRoles
{
    public static readonly RoleSet Contributors = HsAuditRoles.Auditors;

    public static readonly RoleSet AllowedToClose = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.HealthAndSafetyLead);

    public static bool MayClose(IReadOnlyList<Role> roles) => AllowedToClose.IncludesAny(roles);
}
