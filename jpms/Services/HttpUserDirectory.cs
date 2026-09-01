using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Features.Directory;

namespace Jewel.JPMS.Services;

public sealed class HttpUserDirectory : IUserDirectory
{
    private readonly DirectoryReadModel readModel;
    private readonly RevokedDirectoryReadModel revokedReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpUserDirectory(
        DirectoryReadModel readModel,
        RevokedDirectoryReadModel revokedReadModel,
        IQueryClient queries,
        ICommandSender commands)
    {
        this.readModel = readModel;
        this.revokedReadModel = revokedReadModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
        revokedReadModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public bool IsLoaded => readModel.Current is not null;

    public bool IsRevokedLoaded => revokedReadModel.Current is not null;

    public IReadOnlyList<DirectoryUser> All()
    {
        if (readModel.Current is null) _ = readModel.RefreshAsync(CancellationToken.None);
        return readModel.Current ?? Array.Empty<DirectoryUser>();
    }

    public IReadOnlyList<RevokedDirectoryUser> Revoked()
    {
        if (revokedReadModel.Current is null) _ = revokedReadModel.RefreshAsync(CancellationToken.None);
        return revokedReadModel.Current ?? Array.Empty<RevokedDirectoryUser>();
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
            new UpsertDirectoryUser(user.Email, user.DisplayName, user.Roles, user.RevertToOwnRole), cancellationToken);
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
        // Revoking moves the user from one list to the other, so refresh both — but only refresh
        // the revoked list if something is actually reading it (its endpoint is admin-gated, and
        // an FD can revoke without ever opening the Admin area).
        await readModel.RefreshAsync(cancellationToken);
        if (revokedReadModel.Current is not null) await revokedReadModel.RefreshAsync(cancellationToken);
    }

    public async Task RestoreAsync(string email, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new RestoreDirectoryUser(email), cancellationToken);
        // Restoring moves them back the other way.
        await revokedReadModel.RefreshAsync(cancellationToken);
        await readModel.RefreshAsync(cancellationToken);
    }

    public async Task DeleteAsync(string email, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new DeleteDirectoryUser(email), cancellationToken);
        await revokedReadModel.RefreshAsync(cancellationToken);
    }
}
