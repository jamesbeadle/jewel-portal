using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Clients;

public sealed record ListClients() : IQuery<IReadOnlyList<Client>>;
