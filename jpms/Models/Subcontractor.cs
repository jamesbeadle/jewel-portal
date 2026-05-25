namespace Jewel.JPMS.Models;

public enum ComplianceStatus
{
    Current,
    ExpiringSoon,
    Expired,
    Missing
}

public sealed record Subcontractor(
    string SubcontractorId,
    string CompanyName,
    string PrimaryTrade,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CisStatus,
    DateTimeOffset OnboardedAt);

public sealed record ComplianceDocument(
    string ComplianceDocumentId,
    string SubcontractorId,
    string Kind,
    string FileName,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset UploadedAt);

public static class ComplianceDocumentExtensions
{
    public static ComplianceStatus Status(this ComplianceDocument document)
    {
        if (document.ExpiresAt is null) return ComplianceStatus.Current;
        var daysToExpiry = (document.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays;
        if (daysToExpiry < 0) return ComplianceStatus.Expired;
        if (daysToExpiry < 30) return ComplianceStatus.ExpiringSoon;
        return ComplianceStatus.Current;
    }
}

public static class ComplianceStatusExtensions
{
    public static string DisplayName(this ComplianceStatus status) => status switch
    {
        ComplianceStatus.Current      => "Current",
        ComplianceStatus.ExpiringSoon => "Expiring soon",
        ComplianceStatus.Expired      => "Expired",
        ComplianceStatus.Missing      => "Missing",
        _ => status.ToString()
    };

    public static string PillClass(this ComplianceStatus status) => status switch
    {
        ComplianceStatus.Current      => "bg-emerald-50 border-emerald-200 text-emerald-800",
        ComplianceStatus.ExpiringSoon => "bg-amber-50 border-amber-200 text-amber-800",
        ComplianceStatus.Expired      => "bg-rose-50 border-rose-200 text-rose-800",
        ComplianceStatus.Missing      => "bg-slate-100 border-slate-200 text-slate-700",
        _ => "bg-slate-100 border-slate-200 text-slate-700"
    };
}
