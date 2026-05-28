using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Directory;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpUserDirectory : IUserDirectory
{
    private readonly DirectoryReadModel readModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpUserDirectory(DirectoryReadModel readModel, IQueryClient queries, ICommandSender commands)
    {
        this.readModel = readModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<DirectoryUser> All()
    {
        if (readModel.Current is null) _ = readModel.RefreshAsync(CancellationToken.None);
        return readModel.Current ?? Array.Empty<DirectoryUser>();
    }

    public DirectoryUser? Find(string email) =>
        All().FirstOrDefault(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));

    public Task<DirectoryUser?> FindAsync(string email, CancellationToken cancellationToken) =>
        queries.AskAsync(new GetDirectoryUser(email), cancellationToken);

    public DirectoryUser Upsert(DirectoryUser user)
    {
        _ = UpsertAsync(user);
        return user;
    }

    public bool Remove(string email)
    {
        _ = RemoveAsync(email);
        return true;
    }

    private async Task UpsertAsync(DirectoryUser user)
    {
        await commands.SendAsync(new UpsertDirectoryUser(user.Email, user.DisplayName, user.Roles), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    private async Task RemoveAsync(string email)
    {
        await commands.SendAsync(new RemoveDirectoryUser(email), CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }
}
