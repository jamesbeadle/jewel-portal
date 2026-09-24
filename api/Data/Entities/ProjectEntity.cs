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
    // The site manager who answers for the site (2026-09-23): a person, not a login — the name
    // the audit's front sheet pre-fills, and the address the H&S digest of what the officer did
    // on his site goes to. Blank means nobody is told.
    [MaxLength(256)]     public string SiteManagerName { get; set; } = "";
    [MaxLength(256)]     public string SiteManagerEmail { get; set; } = "";
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

    // With a ValuationCycle this is the anchor the cycle counts from (ValuationSchedule).
    public DateTimeOffset? NextExpectedValuationDate { get; set; }

    public int ValuationCycle { get; set; }

    // The FD's forecast assumption (2026-08-13): roughly how much the architect is expected to
    // certify per valuation month on this project. Null = no view (the Cash Forecast spreads
    // left-to-claim evenly to practical completion); set, the forecast claims at this rate until
    // the money runs out. Edited inline on the Cash Forecast page; forecasting only.
    public decimal? ExpectedMonthlyValuation { get; set; }

    // Site address — Town + Postcode drive the "find local subcontractors" search near the project.
    [MaxLength(256)]     public string AddressLine { get; set; } = "";
    [MaxLength(128)]     public string Town { get; set; } = "";
    [MaxLength(16)]      public string Postcode { get; set; } = "";

    // The project's option in Xero's "Sites" tracking category, exactly as named in Xero
    // (e.g. "21 Chetwode Road"). Explicit mapping — set on the project details editor. The
    // Xero write-back stamps this on every line allocated to the project and fails loudly
    // when it's missing, rather than guessing and writing a wrong site into the accounts.
    [MaxLength(128)]     public string? XeroSiteName { get; set; }

    // The Xero customer the project's sales invoices are raised on (2026-09-10, the accountant's
    // ask: the raise matched the client by NAME and created a duplicate contact on a miss).
    // Xero's ContactID and the name as Xero holds it, mapped explicitly in Project settings like
    // XeroSiteName. Raise in Xero is blocked until the id is set — it never matches by name and
    // never creates a contact.
    [MaxLength(64)]      public string? XeroContactId { get; set; }
    [MaxLength(256)]     public string? XeroContactName { get; set; }

    // Retired 2026-09-22 with the Import WhatsApp week page: nothing reads or writes it, and the
    // column stays only so the schema needs no migration.
    [MaxLength(1024)]    public string? SiteNoteSenderNames { get; set; }
}
