
namespace Jewel.JPMS.Services;

public interface IUserDirectory
{
    DirectoryUser? Find(string email);

    Task<DirectoryUser?> FindAsync(string email, CancellationToken cancellationToken);

    bool IsApproved(string email) => Find(email) is not null;

    IReadOnlyList<DirectoryUser> All();

    /// <summary>
    /// False until the first fetch of the directory has landed. <see cref="All"/> answers with an
    /// empty list in the meantime, which is indistinguishable from "there really are no users" —
    /// so anything that would render a count, a table or an empty state has to check this first and
    /// show a loading indicator instead.
    /// </summary>
    bool IsLoaded { get; }

    DirectoryUser Upsert(DirectoryUser user);

    /// <summary>Saves the user and waits for both the write and the refreshed list, so a caller can
    /// show a busy state and report a failure instead of guessing that it worked.</summary>
    Task<DirectoryUser> SaveAsync(DirectoryUser user, CancellationToken cancellationToken);

    bool Remove(string email);

    /// <summary>Awaitable counterpart to <see cref="Remove"/>, for callers that need to know it
    /// actually succeeded. Removal is a REVOCATION: the user drops off <see cref="All"/> and can
    /// no longer sign in, but their record survives on <see cref="Revoked"/> until restored or
    /// permanently deleted.</summary>
    Task RemoveAsync(string email, CancellationToken cancellationToken);

    /// <summary>The users whose access has been revoked, most recently revoked first. Same
    /// lazy-fetch contract as <see cref="All"/>: empty until the first fetch lands, so check
    /// <see cref="IsRevokedLoaded"/> before rendering a count, a table or an empty state.</summary>
    IReadOnlyList<RevokedDirectoryUser> Revoked();

    /// <summary>False until the first fetch of the revoked list has landed.</summary>
    bool IsRevokedLoaded { get; }

    /// <summary>Reinstates a revoked user — their roles were kept, so they come back exactly as
    /// they were (with their old password if they had set one).</summary>
    Task RestoreAsync(string email, CancellationToken cancellationToken);

    /// <summary>Permanently deletes a REVOKED user's record. The API refuses this for users who
    /// are still active — revoke first, delete second, always two deliberate steps.</summary>
    Task DeleteAsync(string email, CancellationToken cancellationToken);

    event Action? OnChange;
}
