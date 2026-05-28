using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Api.Cqrs;

public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
