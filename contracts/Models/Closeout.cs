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
    DateTimeOffset? ResolvedAt);

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
