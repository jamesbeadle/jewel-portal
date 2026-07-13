namespace Jewel.JPMS.Models;

// The kind of company a directory record is. Used for filtering — e.g. only Subcontractor (and
// Supplier) records are offered when inviting to a bid package, never Clients or Architects.
// Extensible: add values as more company types are tracked. Subcontractor is the default.
public enum DirectoryCategory
{
    Subcontractor = 0,
    Client = 1,
    Architect = 2,
    Supplier = 3,
    Other = 4
}

public enum ComplianceStatus
{
    Current,
    ExpiringSoon,
    Expired,
    Missing
}

// A trade from the curated master list (e.g. "Bricklayer"). Directory records carry a set of these
// rather than a free-text string, so RFI/bid-package trade filters group reliably.
public sealed record Trade(string TradeId, string Name);

// A company directory record. Originally subcontractor-only; now any company type (see Category).
// The id/field names keep the "Subcontractor" prefix for back-compat with existing references
// (bid-package recipients, compliance docs) while the directory is unified by Category.
public sealed record Subcontractor(
    string SubcontractorId,
    string CompanyName,
    IReadOnlyList<Trade> Trades,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CisStatus,
    DateTimeOffset OnboardedAt,
    DirectoryCategory Category = DirectoryCategory.Subcontractor,
    string MobileNumber = "",
    string Town = "",
    string County = "",
    string Website = "",
    string Pli = "",
    string PliExpiry = "")
{
    // Display helper: the trade names joined for one-line contexts (tables, subtitles).
    public string TradesLabel => string.Join(" · ", Trades.Select(trade => trade.Name));

    public bool HasTrade(string tradeId) =>
        Trades.Any(trade => string.Equals(trade.TradeId, tradeId, StringComparison.OrdinalIgnoreCase));
}

public sealed record ComplianceDocument(
    string ComplianceDocumentId,
    string SubcontractorId,
    string Kind,
    string FileName,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset UploadedAt,
    int Version = 1,
    DateTimeOffset? SupersededAt = null,
    bool HasFile = false,
    long FileSize = 0)
{
    /// <summary>The live version of its Kind. Superseded versions are audit history and should
    /// not drive expiry banners or status pills.</summary>
    public bool IsCurrentVersion => SupersededAt is null;
}

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
