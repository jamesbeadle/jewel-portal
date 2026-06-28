using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Projects;

public sealed record RemoveProjectContact(string ProjectId, string ContactId) : ICommand<Acknowledgement>;
