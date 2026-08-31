using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>Every subcontractor's current-version compliance documents in one read — the feed for
/// the directory list's compliance column and the dashboard's expiring-documents panel. Superseded
/// versions are audit history, never status, so they are excluded at source.</summary>
public sealed record ListCurrentComplianceDocuments() : IQuery<IReadOnlyList<ComplianceDocument>>;
