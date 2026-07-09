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
    string? OnBehalfOfClientId = null,
    // Site address — used to find local subcontractors near the project (and anywhere else the
    // site's location matters). Free-text; Town + Postcode are what the local search needs.
    string AddressLine = "",
    string Town = "",
    string Postcode = "",
    // The project's option in Xero's "Sites" tracking category, exactly as named in Xero.
    // Explicit mapping: the Xero write-back (cost-code confirmation + invoice approval)
    // stamps it on every allocated line and refuses to run while it's unset.
    string? XeroSiteName = null);
