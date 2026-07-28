using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

public sealed record UpdateSubcontractor(
    string SubcontractorId,
    string CompanyName,
    IReadOnlyList<string> TradeIds,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CisStatus,
    // Payment terms printed on the company's purchase orders ("30 day terms"). Null means
    // "leave unchanged", so callers that only touch other fields never reset an override.
    int? PaymentTermsDays = null,
    // Postal address (street line(s), town, county, postcode) printed letter-style at the top
    // of the company's purchase orders. Null means "leave unchanged" — same rule as
    // PaymentTermsDays, so trade-only saves never wipe an address; empty string clears a field.
    string? AddressLine = null,
    string? Town = null,
    string? County = null,
    string? Postcode = null) : ICommand<Subcontractor>;
