using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>Section 1 as the raw notes read, whatever rewrite the report holds — what a rewrite is
/// asked from, so asking twice never rewrites a rewrite.</summary>
public sealed record ContractorsReportRawWeek(ContractorsReportHeader Header, IReadOnlyList<ContractorsReportDay> Days, ReportingWeek Week);
