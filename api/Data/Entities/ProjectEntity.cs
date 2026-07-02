using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class ProjectEntity
{
    [Key, MaxLength(64)] public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string Reference { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(256)]     public string ClientName { get; set; } = "";
    public int Organisation { get; set; }
    public int Stage { get; set; }
    [MaxLength(256)]     public string ProjectManagerEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    // The party this project corresponds with: a client account directly (PartyKind 0) or an
    // architect acting on a client's behalf (PartyKind 1, with OnBehalfOfClientId optionally
    // recording that client). Where project emails (RFIs etc.) are addressed; requests fall back
    // to this when they carry no party link of their own. PartyId is null until assigned; the
    // free-text ClientName above remains the display name shown on documents.
    public int PartyKind { get; set; }
    [MaxLength(64)]      public string? PartyId { get; set; }
    [MaxLength(64)]      public string? OnBehalfOfClientId { get; set; }

    // Running total of valuation invoices received from the client on this project. Incremented when a cash
    // call is marked Received. Denormalised for the directors' project-level view.
    public decimal ValuationInvoicePaidTotal { get; set; }
}
