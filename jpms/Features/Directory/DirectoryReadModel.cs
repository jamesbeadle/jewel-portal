using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Features.Directory;

public sealed class DirectoryReadModel : IReadModelStore<IReadOnlyList<DirectoryUser>>
{
    private readonly IQueryClient queries;

    public DirectoryReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<DirectoryUser>? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListDirectoryUsers(), cancellationToken);
        OnChanged?.Invoke();
    }
}
