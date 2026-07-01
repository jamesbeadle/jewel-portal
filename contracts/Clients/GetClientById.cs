using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Clients;

public sealed record GetClientById(string ClientId) : IQuery<Client?>;
