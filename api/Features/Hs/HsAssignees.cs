namespace Jewel.JPMS.Api.Features.Hs;

/// <summary>An H&S record is assigned to a portal user by email or to a person by name — the
/// site manager on an audit sheet's Owner column has no login (2026-09-15). One or the other.</summary>
internal static class HsAssignees
{
    public static bool IsUnassigned(string email, string name) =>
        string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(name);
}
