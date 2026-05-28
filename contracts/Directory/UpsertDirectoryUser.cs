using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Directory;

public sealed record UpsertDirectoryUser(
    string Email,
    string DisplayName,
    IReadOnlyList<Role> Roles) : ICommand<DirectoryUser>;
