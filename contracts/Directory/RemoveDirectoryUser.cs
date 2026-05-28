using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Directory;

public sealed record RemoveDirectoryUser(string Email) : ICommand<Acknowledgement>;
