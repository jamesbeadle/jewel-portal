using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Directory;

/// <summary>Revokes a user's access. This is a soft removal: the directory record and role list
/// survive (see RevokedDirectoryUser) so an administrator can restore them, but they can no
/// longer sign in and they disappear from every active-user list. RevokedBy is stamped
/// server-side from the signed-in administrator — the client sends only the email.</summary>
public sealed record RemoveDirectoryUser(string Email, string RevokedBy = "") : ICommand<Acknowledgement>;
