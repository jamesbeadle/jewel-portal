namespace Jewel.JPMS.Models;

/// <summary>
/// Which compliance documents are insurance, and when cover has lapsed: a current certificate whose
/// kind names insurance ("Public liability insurance", "Insurance") and whose expiry has passed. The
/// renewal chase asks for the same certificates before they lapse, and the compliance register lists
/// the companies still on site after one has.
/// </summary>
public static class ComplianceInsurance
{
    public const string KindWord = "insurance";

    public static bool IsInsurance(this ComplianceDocument document) =>
        document.Kind.Contains(KindWord, StringComparison.OrdinalIgnoreCase);

    public static bool HasLapsed(this ComplianceDocument document) =>
        document.IsCurrentVersion && document.IsInsurance() && document.Status() == ComplianceStatus.Expired;
}
