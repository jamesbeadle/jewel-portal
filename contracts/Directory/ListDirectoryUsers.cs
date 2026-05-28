using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Directory;

public sealed record ListDirectoryUsers : IQuery<IReadOnlyList<DirectoryUser>>;
