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

    public bool IsLoaded => readModel.Current is not null;

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
        _ = SaveAsync(user, CancellationToken.None);
        return user;
    }

    public async Task<DirectoryUser> SaveAsync(DirectoryUser user, CancellationToken cancellationToken)
    {
        var saved = await commands.SendAsync(
            new UpsertDirectoryUser(user.Email, user.DisplayName, user.Roles), cancellationToken);
        await readModel.RefreshAsync(cancellationToken);
        return saved;
    }

    public bool Remove(string email)
    {
        _ = RemoveAsync(email, CancellationToken.None);
        return true;
    }

    public async Task RemoveAsync(string email, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new RemoveDirectoryUser(email), cancellationToken);
        await readModel.RefreshAsync(cancellationToken);
    }
}
