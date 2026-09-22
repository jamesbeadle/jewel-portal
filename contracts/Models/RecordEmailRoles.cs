namespace Jewel.JPMS.Models;

/// <summary>
/// Who may read the mail stored behind a record — the emails tagged to an RFI, a variation, a
/// work order, a valuation period. The one rule for the hard line the business draws: internal
/// staff read it, and no external login (client, architect, subcontractor) ever does. The API
/// endpoints that serve record mail refuse outside this set, and every widget that renders it
/// renders nothing outside this set, so an external page never depends on a 403 being handled.
/// </summary>
public static class RecordEmailRoles
{
    public static readonly RoleSet Readers = JpmsRoleSets.AllInternal;
}
