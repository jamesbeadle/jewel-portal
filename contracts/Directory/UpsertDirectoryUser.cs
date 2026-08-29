using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Directory;

public sealed record UpsertDirectoryUser(
    string Email,
    string DisplayName,
    IReadOnlyList<Role> Roles,
    // See DirectoryUser.RevertToOwnRole — the "Viewing as" switch defaults back to the user's
    // own role after two hours. Defaults false so existing callers are unchanged.
    bool RevertToOwnRole = false) : ICommand<DirectoryUser>;
