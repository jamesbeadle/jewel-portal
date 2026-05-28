using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record CaptureLead(
    string Reference,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CompanyName,
    string SiteAddress,
    decimal? EstimatedValue,
    LeadSource Source,
    string OwnerEmail) : ICommand<Lead>;
