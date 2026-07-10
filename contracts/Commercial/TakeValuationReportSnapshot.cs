using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Freezes an immutable, line-level copy of the project's valuation report as it stands right now
/// — every priced line with its % complete and cumulative claimed (from the latest open claim, if
/// any), plus the summary/retention footer with "Certified to date" stamped from Issued+Paid
/// invoices at this moment. Taken automatically when an invoice is submitted; taken on demand as a
/// period-end record. Snapshots are immutable once taken — an amendment produces a NEW snapshot.
/// </summary>
public sealed record TakeValuationReportSnapshot(
    string ProjectId,
    string Label,
    string? ValuationInvoiceId = null) : ICommand<ValuationReportSnapshot>;
