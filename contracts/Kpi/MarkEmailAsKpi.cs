using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Kpi;

// Mark a mailbox email as a KPI filed under one person — the Control Centre's Internal-pane
// "Mark as KPI" (administrators only). The server reads the email back from the mailbox to
// snapshot its envelope; nothing is tagged in the mailbox.
//
// The person is named ONE of three ways, resolved in this order (KpiPersonResolver):
//   PersonId    — an existing KpiPerson (list_kpi_people / the page's picker);
//   PersonEmail — a portal user's sign-in email: their KpiPerson is found or created;
//   PersonName  — someone without a portal login ("James Clark"): matched to an existing
//                 name-only person case-insensitively, else created on the spot.
// InternetMessageId lets it re-find the message if its Graph id has changed since the queue was
// rendered. MarkedByEmail is stamped server-side and can never be supplied by the caller.
public sealed record MarkEmailAsKpi(
    string MessageId,
    string? PersonId = null,
    string? PersonEmail = null,
    string? PersonName = null,
    string Note = "",
    string? InternetMessageId = null,
    string MarkedByEmail = "") : ICommand<KpiEmail>;
