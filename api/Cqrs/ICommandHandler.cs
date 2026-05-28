using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Api.Cqrs;

public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
