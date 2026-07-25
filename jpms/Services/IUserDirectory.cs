using Jewel.JPMS.Models;

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
    /// actually succeeded.</summary>
    Task RemoveAsync(string email, CancellationToken cancellationToken);

    event Action? OnChange;
}
