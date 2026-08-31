using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ClientPortal;

/// <summary>An RFI as the client portal shows it: the record's client-facing facts plus the
/// project's display name (resolved server-side — client sessions can't read the projects list).
/// Deliberately narrower than the internal Request model: no internal notes, no value, no
/// mailbox metadata.</summary>
public sealed record ClientPortalRequest(
    string RequestId,
    string ProjectId,
    string ProjectName,
    string Reference,
    string Title,
    string Description,
    RequestType Kind,
    RequestStatus Status,
    DateTimeOffset RaisedAt,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? RespondedAt,
    string? ResponseText);

/// <summary>A variation order as the client portal shows it. Variations are the client's own
/// instruction to spend, so the agreed figures are client-facing by design; the quoting
/// machinery (bid packages, draft lines, cost codes) never travels here.</summary>
public sealed record ClientPortalVariationOrder(
    string VariationOrderId,
    string ProjectId,
    string ProjectName,
    string Reference,
    string? VariationRef,
    string Title,
    string Description,
    VariationOrderStatus Status,
    decimal? EstimatedValue,
    decimal Value,
    DateTimeOffset CreatedAt,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? ApprovedAt);

// Every ClientId below is resolved SERVER-SIDE from the session (Gates/ClientScope) — the client
// sends the query with it empty and can never ask for another client's records. The property
// exists only so the API-side handler receives the resolved id, mirroring the subcontractor
// portal's contracts.

/// <summary>GET /api/client-portal/my/requests — RFIs on the caller's projects, newest first.</summary>
public sealed record ListMyClientRequests(string ClientId = "")
    : IQuery<IReadOnlyList<ClientPortalRequest>>;

/// <summary>GET /api/client-portal/my/requests/{requestId} — one RFI, null when it isn't on one
/// of the caller's projects (indistinguishable from not existing, on purpose).</summary>
public sealed record GetMyClientRequest(string RequestId, string ClientId = "")
    : IQuery<ClientPortalRequest?>;

/// <summary>GET /api/client-portal/my/requests/{requestId}/messages — the request's SHARED
/// in-app thread, oldest first. Internal notes and email legs never travel here.</summary>
public sealed record ListMyClientRequestMessages(string RequestId, string ClientId = "")
    : IQuery<IReadOnlyList<RequestMessage>>;

/// <summary>POST /api/client-portal/my/requests/{requestId}/messages — the client adds to the
/// shared thread. Visibility is forced to Shared and the author is stamped server-side.</summary>
public sealed record PostMyClientRequestMessage(
    string RequestId,
    string Body,
    string? ParentMessageId = null,
    string ClientId = "",
    string AuthorEmail = "",
    string AuthorName = "") : ICommand<RequestMessage>;

/// <summary>GET /api/client-portal/my/variation-orders — variations on the caller's projects
/// that have reached the client (Quoting is internal pricing work), newest first.</summary>
public sealed record ListMyClientVariationOrders(string ClientId = "")
    : IQuery<IReadOnlyList<ClientPortalVariationOrder>>;

/// <summary>GET /api/client-portal/my/variation-orders/{voId} — one variation order, null when
/// it isn't on one of the caller's projects or hasn't reached them yet.</summary>
public sealed record GetMyClientVariationOrder(string VariationOrderId, string ClientId = "")
    : IQuery<ClientPortalVariationOrder?>;

/// <summary>GET /api/client-portal/my/variation-orders/{voId}/messages — the order's SHARED
/// in-app thread, oldest first. Internal notes never travel here.</summary>
public sealed record ListMyClientVariationOrderMessages(string VariationOrderId, string ClientId = "")
    : IQuery<IReadOnlyList<VariationOrderMessage>>;

/// <summary>POST /api/client-portal/my/variation-orders/{voId}/messages — the client adds to the
/// shared thread. Visibility is forced to Shared and the author is stamped server-side.</summary>
public sealed record PostMyClientVariationOrderMessage(
    string VariationOrderId,
    string Body,
    string? ParentMessageId = null,
    string ClientId = "",
    string AuthorEmail = "",
    string AuthorName = "") : ICommand<VariationOrderMessage>;
