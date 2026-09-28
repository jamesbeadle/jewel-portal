using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Directory;

/// <summary>POST /api/directory/password — an administrator sets a user's password by hand from
/// their Admin → Users row (2026-09-28), for someone who cannot use an emailed link. The password
/// meets PasswordPolicy, is stored only as its hash and is never echoed back; SetByEmail is stamped
/// server-side from the signed-in administrator. Deliberately not a connector action: a password
/// is typed on the page, never into a chat.</summary>
public sealed record SetUserPassword(
    string Email,
    string Password,
    string SetByEmail = "") : ICommand<Acknowledgement>;
