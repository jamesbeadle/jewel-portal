using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IArchitectStore
{
    IReadOnlyList<Architect> All();
    event Action? OnChange;

    Task<IReadOnlyList<Architect>> ListAsync(CancellationToken cancellationToken = default);
    Task<Architect?> GetAsync(string architectId, CancellationToken cancellationToken = default);
    Task<Architect> CreateAsync(CreateArchitect command, CancellationToken cancellationToken = default);
    Task<Architect> UpdateAsync(UpdateArchitect command, CancellationToken cancellationToken = default);
}
