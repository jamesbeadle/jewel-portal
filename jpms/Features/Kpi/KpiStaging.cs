namespace Jewel.JPMS.Features.Kpi;

/// <summary>
/// The identity key a staged "Mark as KPI" carries (StagedSystemAction.Key) so the Tagging tab's
/// KPI section and the Actions form's Mark as KPI recognise each other's staging — one person,
/// one mark, whichever tab staged it.
/// </summary>
public static class KpiStaging
{
    public static string KeyFor(string personId) => "kpi:id:" + personId;
    public static string KeyFor(KpiPersonChoice choice) => choice switch
    {
        { PersonId: not null } c => KeyFor(c.PersonId),
        { Email: not null } c => "kpi:email:" + c.Email.ToLowerInvariant(),
        { Name: not null } c => "kpi:name:" + c.Name.Trim().ToLowerInvariant(),
        _ => "kpi:"
    };
}
