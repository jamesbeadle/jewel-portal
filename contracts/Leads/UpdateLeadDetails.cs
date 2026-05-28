using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record UpdateLeadDetails(
    string LeadId,
    string Reference,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CompanyName,
    string SiteAddress,
    decimal? EstimatedValue,
    LeadSource Source,
    LeadStage Stage,
    string OwnerEmail) : ICommand<Lead>;
