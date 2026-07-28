using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>
/// Copies one Xero supplier into the company directory as a new record (category Supplier, no
/// trades — those are curated by hand afterwards). The record is linked to the Xero contact via a
/// SubcontractorXeroLink row, which is what marks it "linked to Xero" in the directory and survives
/// consolidation; Xero's additional contact persons become company contact rows. Importing never
/// merges into an existing record — duplicates are resolved afterwards with
/// <see cref="ConsolidateDirectoryRecords"/>, one consistent flow for all duplicates.
/// </summary>
public sealed record ImportXeroSupplier(string XeroContactId) : ICommand<Subcontractor>;
