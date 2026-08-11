using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.UsefulInformation;

public sealed record ListUsefulInformationForProject(string ProjectId) : IQuery<IReadOnlyList<UsefulInformationNote>>;
