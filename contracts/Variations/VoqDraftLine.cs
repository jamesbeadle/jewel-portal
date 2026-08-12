namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// One suggested scope-of-works line for a drafted variation's first bid package. Mirrors
/// BidPackageLineItemInput so an accepted line maps 1:1 onto the procurement line-item commands.
/// Used by the assistant-drafted variation flow on the request page.
/// </summary>
public sealed record VoqDraftLine(
    string Trade,
    string Description,
    string Unit,
    decimal Quantity);
