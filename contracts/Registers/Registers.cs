using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Registers;

// The Monday replacement (scope §8): company registers with renewal visibility, and staff
// sign-off forms with an evidential trail. Register admin is office/director-gated; signing is
// every user's own surface.

/// <summary>All register rows, active first. The page splits by kind client-side.</summary>
public sealed record ListRegisterItems : IQuery<IReadOnlyList<RegisterItem>>;

/// <summary>Add (empty id) or update (existing id) one register row.</summary>
public sealed record SaveRegisterItem(RegisterItem Item) : ICommand<RegisterItem>;

/// <summary>Deactivates a row — registers keep history, they never hard-delete.</summary>
public sealed record DeactivateRegisterItem(string RegisterItemId) : ICommand<Acknowledgement>;

/// <summary>Every published policy with its signed/outstanding counts (admin view).</summary>
public sealed record ListPolicyDocuments : IQuery<IReadOnlyList<PolicyDocument>>;

/// <summary>Sign-off state per recipient for one policy (admin drill-down + chasing).</summary>
public sealed record ListPolicySignOffs(string PolicyDocumentId) : IQuery<IReadOnlyList<PolicySignOff>>;

/// <summary>
/// Publishes a document for acknowledgement to the named portal users. Publishing a NEW revision
/// of an existing title re-triggers the cycle: fresh sign-off rows, everyone signs again.
/// </summary>
public sealed record PublishPolicyDocument(
    string Title, string Summary, IReadOnlyList<string> RecipientEmails) : ICommand<PolicyDocument>;

/// <summary>The signed-in user's own outstanding (and recently signed) acknowledgements.</summary>
public sealed record ListMyPolicySignOffs : IQuery<IReadOnlyList<PolicySignOff>>;

/// <summary>
/// Signs one of the caller's own acknowledgements: typed name + server timestamp against the
/// document revision — the same evidential pattern as drawing approval. No impersonation: the
/// row must belong to the signed-in email.
/// </summary>
public sealed record SignPolicy(string PolicySignOffId, string TypedName) : ICommand<PolicySignOff>;
