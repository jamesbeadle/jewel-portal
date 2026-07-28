using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>
/// Merges duplicate directory records into one master record. The caller picks the master and the
/// winning value for each field (chosen side by side in the consolidation dialog); the handler then
/// applies those values to the master, unions the trades, re-points everything that referenced a
/// merged record (work orders, bid-package invites and quotes, compliance documents, workers,
/// labour settlement rows, variation orders and requests, portal logins, company contacts and Xero
/// links) and deletes the merged-away records. Contact details belonging to the merged records that
/// didn't win the master's primary-contact fields are kept as company contact rows, so no email or
/// phone number is lost in the merge. A record consolidated from any Xero-linked record stays
/// linked to Xero — the links move to the master.
/// </summary>
public sealed record ConsolidateDirectoryRecords(
    string MasterSubcontractorId,
    // The records merged away — never includes the master.
    IReadOnlyList<string> MergedSubcontractorIds,
    // The winning values, applied to the master record.
    string CompanyName,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CisStatus,
    DirectoryCategory Category,
    string MobileNumber,
    string Town,
    string County,
    string Website,
    int PaymentTermsDays) : ICommand<Subcontractor>;
