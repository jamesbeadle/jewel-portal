namespace Jewel.JPMS.Models;

public sealed record Project(
    string ProjectId,
    string Reference,
    string Name,
    string ClientName,
    Organisation Organisation,
    ProjectStage Stage,
    string ProjectManagerEmail,
    DateTimeOffset CreatedAt,
    // The party this project corresponds with: a client account directly, or an architect acting
    // on a client's behalf (OnBehalfOfClientId optionally records that client). Null PartyId until
    // assigned; ClientName above stays the free-text display name shown on documents.
    PartyKind PartyKind = PartyKind.Client,
    string? PartyId = null,
    string? OnBehalfOfClientId = null);
