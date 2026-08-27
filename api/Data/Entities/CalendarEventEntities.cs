using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jewel.JPMS.Api.Data.Entities;

// One entry on a project's calendar — a site visit, a delivery, a meeting, a subcontractor's
// attendance. Rows are created from the project's Calendar tab or from an email at the triage
// stage. The sequential Number renders as "CAL-0001", which doubles as the mailbox tag stem
// ("JPMS/CAL-0001") — the link between an event and its emails is the tag, never a stored copy.
//
// Date is a UK-local calendar date stored as midnight UTC (the SiteClock rule). StartTime is
// display-only "HH:mm" wall-clock text, so a 09:00 site visit stays 09:00 across DST changes.
// EndDate is the INCLUSIVE last day of a multi-day event; null = a single day. ClientVisible
// marks the event as safe for the client's eyes — client access doesn't exist yet, the flag is
// here from day one so the calendar is client-ready when it does.
public sealed class CalendarEventEntity
{
    [Key, MaxLength(64)] public string CalendarEventId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";

    // Global sequential number behind the "CAL-0001" reference (allocated MAX+1 at create).
    public int Number { get; set; }

    [MaxLength(256)]     public string Title { get; set; } = "";

    // A Models.CalendarEventKind value stored as int (same convention as TenderEnquiries.Status).
    public int Kind { get; set; }

    public DateTimeOffset Date { get; set; }
    [MaxLength(5)]       public string? StartTime { get; set; }
    public DateTimeOffset? EndDate { get; set; }

    [MaxLength(4096)]    public string Notes { get; set; } = "";
    public bool ClientVisible { get; set; }

    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    [NotMapped] public string Reference => $"CAL-{Number:0000}";
}
