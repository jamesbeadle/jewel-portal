using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Cqrs;

public interface IQueryClient
{
    Task<TResult> AskAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
