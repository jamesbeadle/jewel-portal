namespace Jewel.JPMS.Cqrs;

public interface IReadModelStore<TModel>
{
    TModel? Current { get; }

    event Action? OnChanged;

    Task RefreshAsync(CancellationToken cancellationToken);
}
