namespace Jewel.JPMS.Models;

/// <summary>
/// Who may connect an AI client to the portal through MCP: staff alone (Nigel, 2026-09-29: "just
/// don't give them MCP access, internal roles only"). A client's or an architect's login uses the
/// site; the connector's reads are the business's own and are not tailored to a party.
/// </summary>
public static class ConnectorRoles
{
    public static readonly RoleSet AllowedToConnect = JpmsRoleSets.AllInternal;
}
