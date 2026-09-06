using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// Rewrites a lead's details — who, where, what, how much, who owns it and which strategy
/// found it. The whole record is applied as supplied; the stage is NOT here (MoveLeadStage,
/// WinLead), nor the outcome fields the server owns.
/// </summary>
public sealed record UpdateLead(
    string LeadId,
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
    string OwnerEmail) : ICommand<Lead>;
