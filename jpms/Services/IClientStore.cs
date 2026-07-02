using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IClientStore
{
    IReadOnlyList<Client> All();
    event Action? OnChange;

    Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default);
    Task<Client?> GetAsync(string clientId, CancellationToken cancellationToken = default);
    Task<Client> CreateAsync(CreateClient command, CancellationToken cancellationToken = default);
    Task<Client> UpdateContactAsync(UpdateClientContact command, CancellationToken cancellationToken = default);
}
