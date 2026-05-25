namespace Jewel.JPMS.Models;

public sealed record Lead(
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
    string OwnerEmail,
    DateTimeOffset CapturedAt);
