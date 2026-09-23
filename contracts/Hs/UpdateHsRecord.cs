using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Hs;

/// <summary>
/// The record replaced whole. ChangedByEmail / ChangedByName are stamped by the door it came
/// through (the endpoint from the sign-in, the connector from the caller) so a status change is
/// on the thread's events as that person's — the digest to the other side reads them. Closing a
/// corrective action is the H&amp;S officer's (HsActionRoles), consulted by the endpoint and the
/// connector alike, never by the handler.
/// </summary>
public sealed record UpdateHsRecord(
    string HsRecordId,
    string Summary,
    HsSeverity Severity,
    HsStatus Status,
    string AssignedToEmail,
    DateTimeOffset? DueAt,
    string AssignedToName = "",
    string ChangedByEmail = "",
    string ChangedByName = "") : ICommand<HsRecord>;
