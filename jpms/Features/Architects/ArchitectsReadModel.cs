using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Architects;

public sealed class ArchitectsReadModel
{
    private readonly IQueryClient queries;
    private IReadOnlyList<Architect> architects = Array.Empty<Architect>();

    public ArchitectsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<Architect> Current => architects;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        architects = await queries.AskAsync(new ListArchitects(), cancellationToken);
        OnChanged?.Invoke();
    }
}
