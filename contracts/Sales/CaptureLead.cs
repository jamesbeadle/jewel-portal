using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// Adds a lead to the register. Source says how it came to exist; when it is
/// <see cref="LeadSource.Strategy"/> the StrategyId names the strategy that found it (the
/// server sets Source to Strategy whenever a StrategyId is given). The LD-#### reference is
/// minted server-side. Stage starts at New unless a warmer stage is given (an inbound enquiry
/// is already Engaged).
/// </summary>
public sealed record CaptureLead(
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CompanyName,
    LeadProspectKind ProspectKind,
    string PropertyAddress,
    string Postcode,
    string Summary,
    string Notes,
    LeadSource Source,
    string? StrategyId,
    decimal? EstimatedValue,
    string OwnerEmail,
    LeadStage Stage = LeadStage.New) : ICommand<Lead>;
