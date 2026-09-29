
namespace Jewel.JPMS.Features.Manual;

/// <summary>The office master, cached until the next refresh; the pages re-read it after every write.</summary>
public sealed class ManualModulesReadModel
{
    private readonly IQueryClient queries;
    public ManualModulesReadModel(IQueryClient queries) { this.queries = queries; }
    public event Action? OnChanged;
    public IReadOnlyList<ManualModule>? Current { get; private set; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListManualModules(), cancellationToken);
        OnChanged?.Invoke();
    }
}
