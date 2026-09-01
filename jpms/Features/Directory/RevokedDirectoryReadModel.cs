using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Features.Directory;

/// <summary>The revoked-users list behind Admin → Users → Revoked. Separate from
/// DirectoryReadModel because the two lists answer different questions ("who can sign in" vs
/// "who did we shut out") and are gated differently on the API.</summary>
public sealed class RevokedDirectoryReadModel : IReadModelStore<IReadOnlyList<RevokedDirectoryUser>>
{
    private readonly IQueryClient queries;

    public RevokedDirectoryReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<RevokedDirectoryUser>? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListRevokedDirectoryUsers(), cancellationToken);
        OnChanged?.Invoke();
    }
}
