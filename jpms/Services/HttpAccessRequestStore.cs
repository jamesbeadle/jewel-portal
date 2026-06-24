using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Directory;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpAccessRequestStore : IAccessRequestStore
{
    private readonly AccessRequestsReadModel readModel;
    private readonly ICommandSender commands;

    public HttpAccessRequestStore(AccessRequestsReadModel readModel, ICommandSender commands)
    {
        this.readModel = readModel;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<AccessRequest> Pending()
    {
        if (readModel.Current is null) _ = readModel.RefreshAsync(CancellationToken.None);
        return readModel.Current ?? Array.Empty<AccessRequest>();
    }

    public AccessRequest Submit(AuthenticatedUser user)
    {
        _ = SubmitAsync(user);
        return new AccessRequest(user.Email, user.DisplayName, DateTimeOffset.UtcNow);
    }

    public bool Remove(string email)
    {
        _ = RemoveAsync(email);
        return true;
    }

    private async Task SubmitAsync(AuthenticatedUser user)
    {
        await commands.SendAsync(new SubmitAccessRequest(user.Email, user.DisplayName), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    private async Task RemoveAsync(string email)
    {
        await commands.SendAsync(new ResolveAccessRequest(email), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }
}
