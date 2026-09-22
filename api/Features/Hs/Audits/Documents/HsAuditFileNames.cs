namespace Jewel.JPMS.Api.Features.Hs.Audits.Documents;

/// <summary>What the downloaded report is called: the site, the reference and the inspection
/// date, so a folder of them sorts by site and reads at a glance.</summary>
public static class HsAuditFileNames
{
    public static string Pdf(HsAudit audit, string projectName)
    {
        var site = string.Concat(projectName.Where(character => char.IsLetterOrDigit(character) || character == ' ')).Trim().Replace(' ', '-');
        return $"{site}-{audit.Reference}-HS-inspection-{audit.InspectionDate:yyyy-MM-dd}.pdf";
    }
}
