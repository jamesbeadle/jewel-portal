namespace Jewel.JPMS.Models;

public enum DefectStatus
{
    Open,
    InProgress,
    Resolved,
    Verified
}

public sealed record Defect(
    string DefectId,
    string ProjectId,
    string Description,
    string Location,
    string AssignedToEmail,
    DefectStatus Status,
    DateTimeOffset RaisedAt,
    DateTimeOffset? ResolvedAt,
    // Sequential human reference ("DEF-0001") — also the mailbox tag stem ("JPMS/DEF-0001"), so a
    // triage email can be filed to the defect and the defect reads its mail back live by tag.
    // Defaulted last so existing construction sites keep compiling; the server always mints it.
    string Reference = "");

public static class DefectStatusExtensions
{
    // UI wording for a status — the enum's InProgress must never leak into copy.
    public static string DisplayName(this DefectStatus status) => status switch
    {
        DefectStatus.Open       => "Open",
        DefectStatus.InProgress => "In progress",
        DefectStatus.Resolved   => "Resolved",
        DefectStatus.Verified   => "Verified",
        _ => status.ToString()
    };
}

public sealed record PracticalCompletion(
    string PracticalCompletionId,
    string ProjectId,
    DateTimeOffset AchievedAt,
    string? CertificateBlobRef,
    string IssuedByEmail,
    bool IsClientSigned);

public sealed record HandoverPackItem(
    string HandoverPackItemId,
    string ProjectId,
    string Label,
    string Detail,
    bool IsReady,
    string? EvidenceBlobRef);

public sealed record SettlementRecord(
    string SettlementRecordId,
    string ProjectId,
    decimal FinalContractValue,
    decimal FinalCost,
    decimal FinalMargin,
    DateTimeOffset AgreedAt,
    bool IsClientSigned);

public sealed record VatAnalysis(
    string VatAnalysisId,
    string ProjectId,
    decimal ZeroRatedAmount,
    decimal StandardRatedAmount,
    string Notes,
    bool IsClientConfirmed,
    bool IsArchitectConfirmed);

public sealed record RetentionRelease(
    string RetentionReleaseId,
    string ProjectId,
    decimal Amount,
    DateTimeOffset ReleasedAt,
    bool IsPublishedDownstream);
