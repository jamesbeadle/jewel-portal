using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Cqrs;

public interface ICommandSender
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken);
}
