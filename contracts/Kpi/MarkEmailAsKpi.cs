using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Kpi;

// Mark a mailbox email as a KPI filed under one person — the Control Centre's Internal-pane
// "Mark as KPI" (administrators only). The server reads the email back from the mailbox to
// snapshot its envelope, then tags it JPMS/Admin (+ the Internal pathway) so it leaves the
// triage queue — the tag says only that an administrator dealt with it; the KPI itself stays in
// the administrators-only register (decision 2026-09-03: the queue is "Inbox without a JPMS tag",
// so an untagged KPI email would sit in the queue forever).
//
// The person is named ONE of three ways, resolved in this order (KpiPersonResolver):
//   PersonId    — an existing KpiPerson (list_kpi_people / the page's picker);
//   PersonEmail — a portal user's sign-in email: their KpiPerson is found or created;
//   PersonName  — someone without a portal login ("James Clark"): matched to an existing
//                 name-only person case-insensitively, else created on the spot.
// InternetMessageId lets it re-find the message if its Graph id has changed since the queue was
// rendered. Scope is how far the Admin tag spreads — MessageOnly by default (the KPI is about
// this one email); the Control Centre passes its "Entire thread" answer. MarkedByEmail is stamped
// server-side and can never be supplied by the caller.
public sealed record MarkEmailAsKpi(
    string MessageId,
    string? PersonId = null,
    string? PersonEmail = null,
    string? PersonName = null,
    string Note = "",
    string? InternetMessageId = null,
    string MarkedByEmail = "",
    LinkThreadScope Scope = LinkThreadScope.MessageOnly) : ICommand<KpiEmail>;
